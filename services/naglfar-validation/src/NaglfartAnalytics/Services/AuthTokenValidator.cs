using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace NaglfartAnalytics.Services;

/// <summary>
/// Validates AUTH-TOKEN signatures from auth-service
/// </summary>
public class AuthTokenValidator
{
    private readonly string _signatureKey;
    private readonly ILogger<AuthTokenValidator> _logger;

    public AuthTokenValidator(IConfiguration configuration, ILogger<AuthTokenValidator> logger)
    {
        _signatureKey = configuration["SIGNATURE_KEY"] ?? throw new InvalidOperationException("SIGNATURE_KEY not configured");
        _logger = logger;
    }

    /// <summary>
    /// Validate AUTH-TOKEN from auth-service
    ///
    /// AUTH-TOKEN format (base64-encoded JSON):
    /// {
    ///   "store_id": "store-1",
    ///   "user_id": 123,
    ///   "expired_at": "2025-12-27T16:00:00.000Z",
    ///   "signature": "hmac_sha256_hex"
    /// }
    /// </summary>
    public bool ValidateAuthToken(string authToken, string expectedStoreId, out AuthTokenData? tokenData)
    {
        tokenData = null;

        try
        {
            // Decode base64
            var decodedBytes = Convert.FromBase64String(authToken);
            var decodedJson = Encoding.UTF8.GetString(decodedBytes);

            // Parse JSON
            tokenData = JsonSerializer.Deserialize<AuthTokenData>(decodedJson);
            if (tokenData == null)
            {
                _logger.LogWarning("AUTH-TOKEN JSON deserialization returned null");
                return false;
            }

            // Validate required fields
            if (string.IsNullOrEmpty(tokenData.StoreId) ||
                string.IsNullOrEmpty(tokenData.ExpiredAt) ||
                string.IsNullOrEmpty(tokenData.Signature))
            {
                _logger.LogWarning("AUTH-TOKEN missing required fields");
                return false;
            }

            // Check expiration
            if (!DateTime.TryParse(tokenData.ExpiredAt, out var expiredAt))
            {
                _logger.LogWarning("AUTH-TOKEN has invalid expired_at format: {ExpiredAt}", tokenData.ExpiredAt);
                return false;
            }

            if (expiredAt < DateTime.UtcNow)
            {
                _logger.LogWarning("AUTH-TOKEN expired at {ExpiredAt}, current time {Now}", expiredAt, DateTime.UtcNow);
                return false;
            }

            // Verify signature
            var messageData = new
            {
                store_id = tokenData.StoreId,
                user_id = tokenData.UserId,
                expired_at = tokenData.ExpiredAt
            };

            var message = JsonSerializer.Serialize(messageData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            var expectedSignature = ComputeHmacSha256(message, _signatureKey);

            if (tokenData.Signature != expectedSignature)
            {
                _logger.LogWarning("AUTH-TOKEN signature mismatch. Expected: {Expected}, Got: {Got}",
                    expectedSignature, tokenData.Signature);
                return false;
            }

            // Validate store_id matches expected
            if (tokenData.StoreId != expectedStoreId)
            {
                _logger.LogWarning("AUTH-TOKEN store_id mismatch. Expected: {Expected}, Got: {Got}",
                    expectedStoreId, tokenData.StoreId);
                return false;
            }

            _logger.LogInformation("AUTH-TOKEN validated successfully for user {UserId}, store {StoreId}",
                tokenData.UserId, tokenData.StoreId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating AUTH-TOKEN");
            return false;
        }
    }

    private string ComputeHmacSha256(string message, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);

        // Convert to hex string (lowercase to match Python's hmac.hexdigest())
        return Convert.ToHexString(hashBytes).ToLower();
    }
}

/// <summary>
/// Data class for AUTH-TOKEN
/// </summary>
public class AuthTokenData
{
    public string StoreId { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string ExpiredAt { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
}
