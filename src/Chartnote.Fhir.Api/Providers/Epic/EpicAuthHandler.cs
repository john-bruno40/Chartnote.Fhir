using System.Net.Http.Headers;

namespace Chartnote.Fhir.Api.Providers.Epic;

public class EpicAuthHandler : DelegatingHandler
{
    private readonly EpicTokenService _tokenService;

    public EpicAuthHandler(EpicTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        var token = await _tokenService.GetTokenAsync(ct);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, ct);
    }
}