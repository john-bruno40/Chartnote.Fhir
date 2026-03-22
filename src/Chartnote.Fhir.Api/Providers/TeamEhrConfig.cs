namespace Chartnote.Fhir.Api.Providers;

public class TeamEhrConfig
{
    public string TeamId        { get; set; } = string.Empty;
    public string EhrType       { get; set; } = string.Empty; // "Epic" | "Altera"
    public string FhirBaseUrl   { get; set; } = string.Empty;
    public string ClientId      { get; set; } = string.Empty;
    public string ClientSecret  { get; set; } = string.Empty;
    public string? TenantId     { get; set; } // Altera-specific
    public bool   IsActive      { get; set; } = true;
    public string? JwksUrl      { get; set; }
    public string? PrivateKeyPath { get; set; }
    public string? KeyId         { get; set; }
}