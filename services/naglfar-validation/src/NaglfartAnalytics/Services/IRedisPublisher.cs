namespace NaglfartAnalytics.Services;

/// <summary>
/// Interface for publishing messages to Redis pub/sub
/// </summary>
public interface IRedisPublisher
{
    /// <summary>
    /// Publish E-TOKEN creation event to Redis according to event.yaml specification
    /// </summary>
    /// <param name="storeId">Store identifier</param>
    /// <param name="authTokenId">The E-TOKEN value</param>
    /// <param name="clientIp">Client IP address</param>
    /// <param name="userAgent">User agent string</param>
    /// <param name="location">Request path/location</param>
    /// <param name="path">Request path</param>
    /// <param name="query">Query string</param>
    /// <param name="eTokenExpiry">E-TOKEN expiration timestamp</param>
    /// <param name="returnUrl">Original URL user was accessing</param>
    Task PublishETokenEventAsync(
        string storeId,
        string authTokenId,
        string clientIp,
        string userAgent,
        string location,
        string path,
        string query,
        string eTokenExpiry,
        string returnUrl);

    /// <summary>
    /// Publish AUTH-TOKEN validation event to Redis according to event.yaml specification
    /// </summary>
    /// <param name="storeId">Store identifier</param>
    /// <param name="sessionId">Session identifier</param>
    /// <param name="authTokenId">The AUTH-TOKEN value</param>
    /// <param name="status">Validation status (pass or fail)</param>
    /// <param name="clientIp">Client IP address</param>
    /// <param name="userAgent">User agent string</param>
    /// <param name="location">Request path/location</param>
    /// <param name="path">Request path</param>
    /// <param name="query">Query string</param>
    /// <param name="userId">User ID (optional, from token if validation passed)</param>
    Task PublishAuthTokenValidatedEventAsync(
        string storeId,
        string sessionId,
        string authTokenId,
        string status,
        string clientIp,
        string userAgent,
        string location,
        string path,
        string query,
        int? userId = null);
}
