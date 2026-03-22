using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Chartnote.Fhir.Api.Providers.Epic;

public class EpicTokenService
{
    private readonly TeamEhrConfig _config;
    private readonly ILogger<EpicTokenService> _logger;
    private readonly HttpClient _httpClient;

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    private const string TokenEndpoint =
        "https://fhir.epic.com/interconnect-fhir-oauth/oauth2/token";

    public EpicTokenService(
        TeamEhrConfig config,
        ILogger<EpicTokenService> logger,
        HttpClient httpClient)
    {
        _config     = config;
        _logger     = logger;
        _httpClient = httpClient;
    }

    public async Task<string> GetTokenAsync(CancellationToken ct = default)
    {
        // Return cached token if still valid (with 30s buffer)
        if (_cachedToken is not null && DateTime.UtcNow < _tokenExpiry.AddSeconds(-30))
        {
            _logger.LogDebug("Using cached Epic token for Team {TeamId}", _config.TeamId);
            return _cachedToken;
        }

        _logger.LogInformation(
            "Requesting new Epic token for Team {TeamId}", _config.TeamId);

        var jwt = BuildClientAssertion();

        var form = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type",
                "client_credentials"),
            new KeyValuePair<string, string>("client_assertion_type",
                "urn:ietf:params:oauth:client-assertion-type:jwt-bearer"),
            new KeyValuePair<string, string>("client_assertion", jwt),
        });

        var response = await _httpClient.PostAsync(TokenEndpoint, form, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError(
                "Epic token request failed for Team {TeamId}: {Error}",
                _config.TeamId, error);
            throw new InvalidOperationException(
                $"Epic token request failed: {response.StatusCode} — {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<EpicTokenResponse>(
            cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty token response from Epic");

        _cachedToken = json.AccessToken;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(json.ExpiresIn);

        _logger.LogInformation(
            "Epic token acquired for Team {TeamId}, expires in {Seconds}s",
            _config.TeamId, json.ExpiresIn);

        return _cachedToken;
    }

    private string BuildClientAssertion()
    {
        var privateKeyPath = _config.PrivateKeyPath
            ?? throw new InvalidOperationException("PrivateKeyPath is not configured.");

        var pem = File.ReadAllText(privateKeyPath);
        var rsa = RSA.Create();
        rsa.ImportFromPem(pem);

        var securityKey  = new RsaSecurityKey(rsa) { KeyId = _config.KeyId };
        var credentials  = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha384);

        var now    = DateTime.UtcNow;
        var expiry = now.AddMinutes(5);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, _config.ClientId),
            new Claim(JwtRegisteredClaimNames.Sub, _config.ClientId),
            new Claim(JwtRegisteredClaimNames.Aud, TokenEndpoint),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
        };

        var token = new JwtSecurityToken(
            claims:             claims,
            notBefore:          now,
            expires:            expiry,
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
    
        return tokenString;

    }
}

internal record EpicTokenResponse(
    [property: System.Text.Json.Serialization.JsonPropertyName("access_token")]
    string AccessToken,
    [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    int ExpiresIn
);