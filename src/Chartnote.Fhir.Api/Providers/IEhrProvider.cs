using Hl7.Fhir.Rest;

namespace Chartnote.Fhir.Api.Providers;

public interface IEhrProvider
{
    string ProviderName { get; }
    string TeamId       { get; }
    Task<FhirClient> GetAuthenticatedClientAsync(CancellationToken ct = default);
}