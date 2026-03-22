using Hl7.Fhir.Rest;

namespace Chartnote.Fhir.Api.Providers.Hapi;

public class HapiProvider : IEhrProvider
{
    private readonly TeamEhrConfig _config;
    private readonly ILogger<HapiProvider> _logger;

    public string ProviderName => "Hapi";
    public string TeamId       => _config.TeamId;

    public HapiProvider(TeamEhrConfig config, ILogger<HapiProvider> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task<FhirClient> GetAuthenticatedClientAsync(CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Creating HAPI FHIR client for Team {TeamId} at {Url}",
            _config.TeamId, _config.FhirBaseUrl);

        var settings = new FhirClientSettings
        {
            PreferredFormat    = ResourceFormat.Json,
            VerifyFhirVersion  = false
        };

        var client = new FhirClient(_config.FhirBaseUrl, settings);
        return Task.FromResult(client);
    }
}