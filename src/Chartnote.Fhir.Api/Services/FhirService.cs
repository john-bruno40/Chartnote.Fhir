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
}