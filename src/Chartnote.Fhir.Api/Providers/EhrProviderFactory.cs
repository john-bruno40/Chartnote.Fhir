namespace Chartnote.Fhir.Api.Providers;

public class EhrProviderFactory
{
    private readonly IConfiguration _config;
    private readonly ILoggerFactory _loggerFactory;

    public EhrProviderFactory(IConfiguration config, ILoggerFactory loggerFactory)
    {
        _config        = config;
        _loggerFactory = loggerFactory;
    }

    public IEhrProvider GetProviderForTeam(string teamId)
    {
        var teams = _config
            .GetSection("EhrConnections:Teams")
            .Get<List<TeamEhrConfig>>()
            ?? throw new InvalidOperationException("EhrConnections not configured.");

        var teamConfig = teams.FirstOrDefault(t =>
            t.TeamId == teamId && t.IsActive)
            ?? throw new InvalidOperationException(
                $"No active EHR configuration found for Team '{teamId}'.");

        return teamConfig.EhrType switch
        {
            "Epic" => CreateEpicProvider(teamConfig),
            "Hapi" => new Hapi.HapiProvider(
                          teamConfig,
                          _loggerFactory.CreateLogger<Hapi.HapiProvider>()),
            _ => throw new NotSupportedException(
                     $"EHR type '{teamConfig.EhrType}' is not supported.")
        };
    }

    private Epic.EpicProvider CreateEpicProvider(TeamEhrConfig config)
    {
        var httpClient    = new HttpClient();
        var tokenService  = new Epic.EpicTokenService(
                                config,
                                _loggerFactory.CreateLogger<Epic.EpicTokenService>(),
                                httpClient);

        return new Epic.EpicProvider(
            config,
            tokenService,
            _loggerFactory.CreateLogger<Epic.EpicProvider>());
    }
}