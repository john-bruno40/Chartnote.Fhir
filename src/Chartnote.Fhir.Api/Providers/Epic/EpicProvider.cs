using Hl7.Fhir.Rest;

namespace Chartnote.Fhir.Api.Providers.Epic;

public class EpicProvider : IEhrProvider
{
    private readonly TeamEhrConfig _config;
    private readonly EpicTokenService _tokenService;
    private readonly ILogger<EpicProvider> _logger;

    public string ProviderName => "Epic";
    public string TeamId       => _config.TeamId;

    public EpicProvider(
        TeamEhrConfig config,
        EpicTokenService tokenService,
        ILogger<EpicProvider> logger)
    {
        _config       = config;
        _tokenService = tokenService;
        _logger       = logger;
    }

    public async Task<FhirClient> GetAuthenticatedClientAsync(
    CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating authenticated Epic FHIR client for Team {TeamId}", _config.TeamId);

        // Ensure we have a valid token before creating the client
        await _tokenService.GetTokenAsync(ct);

        var authHandler = new EpicAuthHandler(_tokenService)
        {
            InnerHandler = new HttpClientHandler()
        };

        var settings = new FhirClientSettings
        {
            PreferredFormat   = ResourceFormat.Json,
            VerifyFhirVersion = false
        };

        return new FhirClient(new Uri(_config.FhirBaseUrl), settings, authHandler);
    }
}