namespace Chartnote.Fhir.Api.Models;

public class ChartnotePatient
{
    public string? FhirPatientId { get; set; }
    public string? Mrn           { get; set; }
    public string  FamilyName    { get; set; } = "Unknown";
    public string  GivenName     { get; set; } = "Unknown";
    public DateOnly? DateOfBirth { get; set; }
    public string? EhrSource     { get; set; }  // "Epic" | "Altera" | "Hapi"
    public DateTime LastSyncedAt { get; set; }
}