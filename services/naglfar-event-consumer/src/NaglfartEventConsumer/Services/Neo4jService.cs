using Neo4j.Driver;
using NaglfartEventConsumer.Models;

namespace NaglfartEventConsumer.Services;

/// <summary>
/// Service for storing events in Neo4j graph database
/// </summary>
public class Neo4jService : IAsyncDisposable
{
    private readonly ILogger<Neo4jService> _logger;
    private readonly IDriver _driver;

    public Neo4jService(ILogger<Neo4jService> logger, IConfiguration configuration)
    {
        _logger = logger;

        var uri = configuration["Neo4j:Uri"] ?? "bolt://localhost:7687";
        var username = configuration["Neo4j:Username"] ?? "neo4j";
        var password = configuration["Neo4j:Password"] ?? "naglfar123";

        _logger.LogInformation("Connecting to Neo4j at {Uri}", uri);
        _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(username, password));
    }

    /// <summary>
    /// Store an event in Neo4j according to v2.0 graph model
    /// Event-centric model: Events contain all data, entities are identity nodes only
    /// </summary>
    public async Task StoreEventAsync(NaglfartEvent eventData, string category)
    {
        // Extract all event properties
        var eventId = eventData.GetString("event_id") ?? Guid.NewGuid().ToString();
        var action = eventData.GetString("action") ?? "unknown";
        var status = eventData.GetString("status");
        var timestamp = eventData.GetDateTime("timestamp") ?? DateTime.UtcNow;
        var clientIp = eventData.GetString("client_ip") ?? "unknown";
        var userAgent = eventData.GetString("user_agent");
        var deviceType = eventData.GetString("device_type");
        var path = eventData.GetString("path") ?? "/";
        var query = eventData.GetString("query");
        var sessionId = eventData.GetString("session_id");
        var userId = eventData.GetInt("user_id");
        var storeId = eventData.GetString("store_id");
        var authTokenId = eventData.GetString("auth_token_id");
        var data = eventData.HasProperty("data") ? eventData.Properties["data"].ToString() : null;

        await using var session = _driver.AsyncSession();

        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                // Single comprehensive query that creates Event and all relationships
                var result = await tx.RunAsync(@"
                    // 1. Create Event node with all properties
                    CREATE (e:Event {
                        event_id: $eventId,
                        action: $action,
                        status: $status,
                        timestamp: datetime($timestamp),
                        client_ip: $clientIp,
                        user_agent: $userAgent,
                        device_type: $deviceType,
                        path: $path,
                        query: $query,
                        session_id: $sessionId,
                        user_id: $userId,
                        store_id: $storeId,
                        auth_token_id: $authTokenId,
                        data: $data,
                        archived: false
                    })

                    // 2. MERGE IPAddress (always created)
                    MERGE (ip:IPAddress {address: $clientIp})
                    ON CREATE SET
                        ip.first_seen = datetime($timestamp),
                        ip.last_seen = datetime($timestamp)
                    ON MATCH SET
                        ip.last_seen = datetime($timestamp)
                    CREATE (e)-[:ORIGINATED_FROM {timestamp: datetime($timestamp)}]->(ip)

                    // 3. MERGE Session (if session_id exists)
                    WITH e
                    CALL {
                        WITH e
                        WITH e WHERE e.session_id IS NOT NULL
                        MERGE (s:Session {session_id: e.session_id})
                        ON CREATE SET
                            s.created_at = datetime(e.timestamp),
                            s.last_activity = datetime(e.timestamp)
                        ON MATCH SET
                            s.last_activity = datetime(e.timestamp)
                        CREATE (e)-[:IN_SESSION {timestamp: datetime(e.timestamp)}]->(s)
                    }

                    // 4. MERGE User (if user_id exists)
                    WITH e
                    CALL {
                        WITH e
                        WITH e WHERE e.user_id IS NOT NULL
                        MERGE (u:User {user_id: e.user_id})
                        ON CREATE SET u.created_at = datetime(e.timestamp)
                        CREATE (e)-[:PERFORMED_BY {timestamp: datetime(e.timestamp)}]->(u)
                    }

                    // 5. MERGE Store (if store_id exists)
                    WITH e
                    CALL {
                        WITH e
                        WITH e WHERE e.store_id IS NOT NULL
                        MERGE (st:Store {store_id: e.store_id})
                        ON CREATE SET st.created_at = datetime(e.timestamp)
                        CREATE (e)-[:TARGETED_STORE {
                            timestamp: datetime(e.timestamp),
                            path: e.path,
                            query: e.query
                        }]->(st)
                    }

                    RETURN e.event_id as eventId
                ", new
                {
                    eventId,
                    action,
                    status,
                    timestamp = timestamp.ToString("o"),
                    clientIp,
                    userAgent,
                    deviceType,
                    path,
                    query,
                    sessionId,
                    userId,
                    storeId,
                    authTokenId,
                    data
                });

                await result.ConsumeAsync();
            });

            _logger.LogInformation(
                "Stored event in Neo4j: EventId={EventId}, Action={Action}, IP={ClientIp}",
                eventId, action, clientIp);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to store event in Neo4j: Action={Action}, EventId={EventId}",
                action, eventId);
            throw;
        }
    }

    /// <summary>
    /// Store multiple events in Neo4j in a single batch transaction (v2.0 model)
    /// More efficient than individual inserts - uses UNWIND for batch processing
    /// </summary>
    public async Task StoreBatchAsync(IEnumerable<EventBatchItem> events)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0)
        {
            return;
        }

        await using var session = _driver.AsyncSession();

        try
        {
            // Prepare events for batch insert
            var eventParams = eventList.Select(item =>
            {
                var eventData = item.Event;
                var timestamp = eventData.GetDateTime("timestamp") ?? DateTime.UtcNow;

                return new Dictionary<string, object>
                {
                    { "event_id", eventData.GetString("event_id") ?? Guid.NewGuid().ToString() },
                    { "action", eventData.GetString("action") ?? "unknown" },
                    { "status", eventData.GetString("status") },
                    { "timestamp", timestamp.ToString("o") },
                    { "client_ip", eventData.GetString("client_ip") ?? "unknown" },
                    { "user_agent", eventData.GetString("user_agent") },
                    { "device_type", eventData.GetString("device_type") },
                    { "path", eventData.GetString("path") ?? "/" },
                    { "query", eventData.GetString("query") },
                    { "session_id", eventData.GetString("session_id") },
                    { "user_id", eventData.GetInt("user_id") },
                    { "store_id", eventData.GetString("store_id") },
                    { "auth_token_id", eventData.GetString("auth_token_id") },
                    { "data", eventData.HasProperty("data") ? eventData.Properties["data"].ToString() : null }
                };
            }).ToList();

            await session.ExecuteWriteAsync(async tx =>
            {
                // Batch insert all events with UNWIND
                var result = await tx.RunAsync(@"
                    UNWIND $events AS event

                    // 1. Create Event node
                    CREATE (e:Event {
                        event_id: event.event_id,
                        action: event.action,
                        status: event.status,
                        timestamp: datetime(event.timestamp),
                        client_ip: event.client_ip,
                        user_agent: event.user_agent,
                        device_type: event.device_type,
                        path: event.path,
                        query: event.query,
                        session_id: event.session_id,
                        user_id: event.user_id,
                        store_id: event.store_id,
                        auth_token_id: event.auth_token_id,
                        data: event.data,
                        archived: false
                    })

                    // 2. MERGE IPAddress
                    MERGE (ip:IPAddress {address: event.client_ip})
                    ON CREATE SET
                        ip.first_seen = datetime(event.timestamp),
                        ip.last_seen = datetime(event.timestamp)
                    ON MATCH SET
                        ip.last_seen = datetime(event.timestamp)
                    CREATE (e)-[:ORIGINATED_FROM {timestamp: datetime(event.timestamp)}]->(ip)

                    // 3. MERGE Session (if session_id exists)
                    WITH e, event
                    CALL {
                        WITH e, event
                        WITH e, event WHERE event.session_id IS NOT NULL
                        MERGE (s:Session {session_id: event.session_id})
                        ON CREATE SET
                            s.created_at = datetime(event.timestamp),
                            s.last_activity = datetime(event.timestamp)
                        ON MATCH SET
                            s.last_activity = datetime(event.timestamp)
                        CREATE (e)-[:IN_SESSION {timestamp: datetime(event.timestamp)}]->(s)
                    }

                    // 4. MERGE User (if user_id exists)
                    WITH e, event
                    CALL {
                        WITH e, event
                        WITH e, event WHERE event.user_id IS NOT NULL
                        MERGE (u:User {user_id: event.user_id})
                        ON CREATE SET u.created_at = datetime(event.timestamp)
                        CREATE (e)-[:PERFORMED_BY {timestamp: datetime(event.timestamp)}]->(u)
                    }

                    // 5. MERGE Store (if store_id exists)
                    WITH e, event
                    CALL {
                        WITH e, event
                        WITH e, event WHERE event.store_id IS NOT NULL
                        MERGE (st:Store {store_id: event.store_id})
                        ON CREATE SET st.created_at = datetime(event.timestamp)
                        CREATE (e)-[:TARGETED_STORE {
                            timestamp: datetime(event.timestamp),
                            path: event.path,
                            query: event.query
                        }]->(st)
                    }

                    RETURN count(e) as events_created
                ", new { events = eventParams });

                await result.ConsumeAsync();
            });

            _logger.LogInformation("Stored batch of {Count} events in Neo4j", eventList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store batch of {Count} events in Neo4j", eventList.Count);
            throw;
        }
    }

    /// <summary>
    /// Verify Neo4j connection
    /// </summary>
    public async Task<bool> VerifyConnectivityAsync()
    {
        try
        {
            await _driver.VerifyConnectivityAsync();
            _logger.LogInformation("Successfully connected to Neo4j");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Neo4j");
            return false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_driver != null)
        {
            await _driver.DisposeAsync();
        }
    }
}
