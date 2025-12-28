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
    /// Store an event in Neo4j as a graph structure
    /// </summary>
    public async Task StoreEventAsync(NaglfartEvent eventData, string category)
    {
        var sessionId = eventData.GetString("session_id");
        var storeId = eventData.GetString("store_id");
        var action = eventData.GetString("action");
        var timestamp = eventData.GetDateTime("timestamp") ?? DateTime.UtcNow;
        var userId = eventData.GetInt("user_id");
        var authTokenId = eventData.GetString("auth_token_id");

        await using var session = _driver.AsyncSession();

        try
        {
            await session.ExecuteWriteAsync(async tx =>
            {
                // Create or merge Session node
                await tx.RunAsync(@"
                    MERGE (s:Session {session_id: $sessionId})
                    ON CREATE SET s.created_at = datetime($timestamp)
                    SET s.last_activity = datetime($timestamp)",
                    new { sessionId, timestamp = timestamp.ToString("o") });

                // Create or merge Store node if store_id exists
                if (!string.IsNullOrEmpty(storeId))
                {
                    await tx.RunAsync(@"
                        MERGE (st:Store {store_id: $storeId})
                        WITH st
                        MATCH (s:Session {session_id: $sessionId})
                        MERGE (s)-[:VISITED_STORE]->(st)",
                        new { storeId, sessionId });
                }

                // Create or merge User node if user_id exists
                if (userId.HasValue)
                {
                    await tx.RunAsync(@"
                        MERGE (u:User {user_id: $userId})
                        WITH u
                        MATCH (s:Session {session_id: $sessionId})
                        MERGE (s)-[:BELONGS_TO_USER]->(u)",
                        new { userId = userId.Value, sessionId });
                }

                // Create Event node with all properties
                var eventProperties = new Dictionary<string, object>
                {
                    { "action", action ?? "unknown" },
                    { "category", category },
                    { "timestamp", timestamp.ToString("o") },
                    { "session_id", sessionId ?? "" }
                };

                if (!string.IsNullOrEmpty(storeId))
                    eventProperties["store_id"] = storeId;

                if (userId.HasValue)
                    eventProperties["user_id"] = userId.Value;

                if (!string.IsNullOrEmpty(authTokenId))
                    eventProperties["auth_token_id"] = authTokenId;

                // Add data field if present
                if (eventData.HasProperty("data"))
                {
                    var dataJson = eventData.Properties["data"].ToString();
                    eventProperties["data"] = dataJson;
                }

                await tx.RunAsync(@"
                    CREATE (e:Event $props)
                    WITH e
                    MATCH (s:Session {session_id: $sessionId})
                    CREATE (s)-[:HAS_EVENT]->(e)",
                    new { props = eventProperties, sessionId });

                // Create relationship to Store if exists
                if (!string.IsNullOrEmpty(storeId))
                {
                    await tx.RunAsync(@"
                        MATCH (e:Event {session_id: $sessionId, timestamp: $timestamp})
                        MATCH (st:Store {store_id: $storeId})
                        CREATE (e)-[:OCCURRED_AT_STORE]->(st)",
                        new { sessionId, timestamp = timestamp.ToString("o"), storeId });
                }

                // Create relationship to User if exists
                if (userId.HasValue)
                {
                    await tx.RunAsync(@"
                        MATCH (e:Event {session_id: $sessionId, timestamp: $timestamp})
                        MATCH (u:User {user_id: $userId})
                        CREATE (e)-[:PERFORMED_BY_USER]->(u)",
                        new { sessionId, timestamp = timestamp.ToString("o"), userId = userId.Value });
                }

                // Create event sequence relationship (ordered by timestamp)
                await tx.RunAsync(@"
                    MATCH (s:Session {session_id: $sessionId})-[:HAS_EVENT]->(events:Event)
                    WITH events ORDER BY events.timestamp DESC LIMIT 2
                    WITH collect(events) as eventList
                    WHERE size(eventList) = 2
                    WITH eventList[1] as current, eventList[0] as previous
                    MERGE (previous)-[:NEXT_EVENT]->(current)",
                    new { sessionId });
            });

            _logger.LogInformation(
                "Stored event in Neo4j: Action={Action}, Category={Category}, SessionId={SessionId}",
                action, category, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to store event in Neo4j: Action={Action}, SessionId={SessionId}",
                action, sessionId);
            throw;
        }
    }

    /// <summary>
    /// Store multiple events in Neo4j in a single batch transaction
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
            await session.ExecuteWriteAsync(async tx =>
            {
                // Process all events in a single transaction
                foreach (var item in eventList)
                {
                    var eventData = item.Event;
                    var category = item.Category;

                    var sessionId = eventData.GetString("session_id");
                    var storeId = eventData.GetString("store_id");
                    var action = eventData.GetString("action");
                    var timestamp = eventData.GetDateTime("timestamp") ?? DateTime.UtcNow;
                    var userId = eventData.GetInt("user_id");
                    var authTokenId = eventData.GetString("auth_token_id");

                    // Create or merge Session node
                    await tx.RunAsync(@"
                        MERGE (s:Session {session_id: $sessionId})
                        ON CREATE SET s.created_at = datetime($timestamp)
                        SET s.last_activity = datetime($timestamp)",
                        new { sessionId, timestamp = timestamp.ToString("o") });

                    // Create or merge Store node if store_id exists
                    if (!string.IsNullOrEmpty(storeId))
                    {
                        await tx.RunAsync(@"
                            MERGE (st:Store {store_id: $storeId})
                            WITH st
                            MATCH (s:Session {session_id: $sessionId})
                            MERGE (s)-[:VISITED_STORE]->(st)",
                            new { storeId, sessionId });
                    }

                    // Create or merge User node if user_id exists
                    if (userId.HasValue)
                    {
                        await tx.RunAsync(@"
                            MERGE (u:User {user_id: $userId})
                            WITH u
                            MATCH (s:Session {session_id: $sessionId})
                            MERGE (s)-[:BELONGS_TO_USER]->(u)",
                            new { userId = userId.Value, sessionId });
                    }

                    // Create Event node with all properties
                    var eventProperties = new Dictionary<string, object>
                    {
                        { "action", action ?? "unknown" },
                        { "category", category },
                        { "timestamp", timestamp.ToString("o") },
                        { "session_id", sessionId ?? "" }
                    };

                    if (!string.IsNullOrEmpty(storeId))
                        eventProperties["store_id"] = storeId;

                    if (userId.HasValue)
                        eventProperties["user_id"] = userId.Value;

                    if (!string.IsNullOrEmpty(authTokenId))
                        eventProperties["auth_token_id"] = authTokenId;

                    // Add data field if present
                    if (eventData.HasProperty("data"))
                    {
                        var dataJson = eventData.Properties["data"].ToString();
                        eventProperties["data"] = dataJson;
                    }

                    await tx.RunAsync(@"
                        CREATE (e:Event $props)
                        WITH e
                        MATCH (s:Session {session_id: $sessionId})
                        CREATE (s)-[:HAS_EVENT]->(e)",
                        new { props = eventProperties, sessionId });

                    // Create relationship to Store if exists
                    if (!string.IsNullOrEmpty(storeId))
                    {
                        await tx.RunAsync(@"
                            MATCH (e:Event {session_id: $sessionId, timestamp: $timestamp})
                            MATCH (st:Store {store_id: $storeId})
                            CREATE (e)-[:OCCURRED_AT_STORE]->(st)",
                            new { sessionId, timestamp = timestamp.ToString("o"), storeId });
                    }

                    // Create relationship to User if exists
                    if (userId.HasValue)
                    {
                        await tx.RunAsync(@"
                            MATCH (e:Event {session_id: $sessionId, timestamp: $timestamp})
                            MATCH (u:User {user_id: $userId})
                            CREATE (e)-[:PERFORMED_BY_USER]->(u)",
                            new { sessionId, timestamp = timestamp.ToString("o"), userId = userId.Value });
                    }

                    // Create event sequence relationship (ordered by timestamp)
                    await tx.RunAsync(@"
                        MATCH (s:Session {session_id: $sessionId})-[:HAS_EVENT]->(events:Event)
                        WITH events ORDER BY events.timestamp DESC LIMIT 2
                        WITH collect(events) as eventList
                        WHERE size(eventList) = 2
                        WITH eventList[1] as current, eventList[0] as previous
                        MERGE (previous)-[:NEXT_EVENT]->(current)",
                        new { sessionId });
                }
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
