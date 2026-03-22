using Hl7.Fhir.Model;
using Hl7.Fhir.Rest;
using Chartnote.Fhir.Api.Models;
using Chartnote.Fhir.Api.Providers;

namespace Chartnote.Fhir.Api.Services;

public class FhirService
{
    private readonly EhrProviderFactory _providerFactory;
    private readonly ILogger<FhirService> _logger;

    public FhirService(EhrProviderFactory providerFactory, ILogger<FhirService> logger)
    {
        _providerFactory = providerFactory;
        _logger          = logger;
    }

    public async Task<List<ScheduleEntry>> GetDailyScheduleAsync(
        string teamId,
        string patientFhirId,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProviderForTeam(teamId);
        var client   = await provider.GetAuthenticatedClientAsync(ct);

        var dateParam = date?.ToString("yyyy-MM-dd");
        _logger.LogInformation(
            "[{Provider}] Fetching schedule for practitioner {Id} on {Date}",
            provider.ProviderName, patientFhirId, dateParam);

        var query = new SearchParams().LimitTo(50);
        query.Add("patient", patientFhirId);
        
        if (date.HasValue)
            query.Add("date", $"ge{date.Value:yyyy-MM-dd}");

        Bundle? bundle;
        try
        {
            bundle = await client.SearchAsync<Appointment>(query);
        }
        catch (FhirOperationException ex)
        {
            _logger.LogError(ex, "FHIR search failed for Appointment");
            throw;
        }

        var entries = new List<ScheduleEntry>();

        foreach (var entry in bundle?.Entry ?? [])
        {
            if (entry.Resource is not Appointment appt) continue;

            var scheduleEntry = new ScheduleEntry
            {
                AppointmentFhirId = appt.Id,
                Start             = appt.Start?.UtcDateTime ?? DateTime.MinValue,
                End               = appt.End?.UtcDateTime,
                Status            = appt.Status?.ToString(),
                AppointmentType   = appt.AppointmentType?.Text,
                EhrSource         = provider.ProviderName
            };

            var patientRef = appt.Participant
                .FirstOrDefault(p => p.Actor?.Reference?.StartsWith("Patient/") == true)
                ?.Actor?.Reference;

            if (patientRef is not null)
            {
                var fhirPatientId = patientRef.Replace("Patient/", "");
                scheduleEntry.Patient = await GetPatientAsync(
                    teamId, fhirPatientId, ct);
            }

            entries.Add(scheduleEntry);
        }

        _logger.LogInformation("Resolved {Count} schedule entries", entries.Count);
        return entries;
    }

    public async Task<ChartnotePatient?> GetPatientAsync(
        string teamId,
        string fhirPatientId,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProviderForTeam(teamId);
        var client   = await provider.GetAuthenticatedClientAsync(ct);

        Patient? patient;
        try
        {
            patient = await client.ReadAsync<Patient>($"Patient/{fhirPatientId}");
        }
        catch (FhirOperationException ex)
        {
            _logger.LogWarning(ex, "Could not fetch Patient/{Id}", fhirPatientId);
            return null;
        }
        if (patient is null) return null;
        return MapToChartnotePatient(patient, provider.ProviderName);
    }

    public async Task<List<ChartnotePatient>> SearchPatientsAsync(
        string teamId,
        string? familyName = null,
        string? givenName = null,
        DateOnly? dateOfBirth = null,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProviderForTeam(teamId);
        var client   = await provider.GetAuthenticatedClientAsync(ct);

        var query = new SearchParams().LimitTo(20);

        if (familyName is not null)
            query.Add("family", familyName);
        if (givenName is not null)
            query.Add("given", givenName);
        if (dateOfBirth.HasValue)
            query.Add("birthdate", dateOfBirth.Value.ToString("yyyy-MM-dd"));

        _logger.LogInformation(
            "[{Provider}] Searching patients family={Family} given={Given}",
            provider.ProviderName, familyName, givenName);

        Bundle? bundle;
        try
        {
            bundle = await client.SearchAsync<Patient>(query);
        }
        catch (FhirOperationException ex)
        {
            _logger.LogError(ex, "FHIR Patient search failed");
            throw;
        }

        var results = new List<ChartnotePatient>();
        foreach (var entry in bundle?.Entry ?? [])
        {
            if (entry.Resource is not Patient patient) continue;
            results.Add(MapToChartnotePatient(patient, provider.ProviderName));
        }

        _logger.LogInformation("Found {Count} patients", results.Count);
        return results;
    }

    private static ChartnotePatient MapToChartnotePatient(
        Patient patient, string ehrSource)
    {
        var name = patient.Name
            .FirstOrDefault(n => n.Use == HumanName.NameUse.Official)
            ?? patient.Name.FirstOrDefault();

        var mrn = patient.Identifier
            .FirstOrDefault(i => i.Type?.Coding?.Any(c => c.Code == "MR") == true)
            ?.Value;

        DateOnly? dob = null;
        if (patient.BirthDate is not null &&
            DateOnly.TryParse(patient.BirthDate, out var parsed))
            dob = parsed;

        return new ChartnotePatient
        {
            FhirPatientId = patient.Id,
            Mrn           = mrn,
            FamilyName    = name?.Family ?? "Unknown",
            GivenName     = name?.Given?.FirstOrDefault() ?? "Unknown",
            DateOfBirth   = dob,
            EhrSource     = ehrSource,
            LastSyncedAt  = DateTime.UtcNow
        };
    }

    public async Task<List<object>> SearchEncountersAsync(
        string teamId,
        string patientFhirId,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProviderForTeam(teamId);
        var client   = await provider.GetAuthenticatedClientAsync(ct);

        _logger.LogInformation(
            "[{Provider}] Searching encounters for patient {Id}",
            provider.ProviderName, patientFhirId);

        var query = new SearchParams()
            .LimitTo(50);
        query.Add("patient", patientFhirId);

        Bundle? bundle;
        try
        {
            bundle = await client.SearchAsync<Encounter>(query);
        }
        catch (FhirOperationException ex)
        {
            _logger.LogError(ex, "FHIR Encounter search failed");
            throw;
        }

        var results = new List<object>();
        foreach (var entry in bundle?.Entry ?? [])
        {
            if (entry.Resource is not Encounter enc) continue;
            results.Add(new
            {
                encounterFhirId = enc.Id,
                status          = enc.Status?.ToString(),
                type            = enc.Type?.FirstOrDefault()?.Text,
                periodStart     = enc.Period?.Start,
                periodEnd       = enc.Period?.End,
                reasonCode      = enc.ReasonCode?.FirstOrDefault()?.Text,
                ehrSource       = provider.ProviderName
            });
        }

        _logger.LogInformation("Found {Count} encounters", results.Count);
        return results;
    }

    public async Task<List<object>> SearchPractitionersAsync(
        string teamId,
        string? familyName = null,
        string? givenName  = null,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.GetProviderForTeam(teamId);
        var client   = await provider.GetAuthenticatedClientAsync(ct);

        _logger.LogInformation(
            "[{Provider}] Searching practitioners family={Family}",
            provider.ProviderName, familyName);

        var query = new SearchParams().LimitTo(20);
        if (familyName is not null) query.Add("family", familyName);
        if (givenName  is not null) query.Add("given",  givenName);

        Bundle? bundle;
        try
        {
            bundle = await client.SearchAsync<Practitioner>(query);
        }
        catch (FhirOperationException ex)
        {
            _logger.LogError(ex, "FHIR Practitioner search failed");
            throw;
        }

        var results = new List<object>();
        foreach (var entry in bundle?.Entry ?? [])
        {
            if (entry.Resource is not Practitioner prac) continue;
            var name = prac.Name.FirstOrDefault();
            results.Add(new
            {
                practitionerFhirId = prac.Id,
                familyName         = name?.Family ?? "Unknown",
                givenName          = name?.Given?.FirstOrDefault() ?? "Unknown",
                ehrSource          = provider.ProviderName
            });
        }

        _logger.LogInformation("Found {Count} practitioners", results.Count);
        return results;
    }

    

}