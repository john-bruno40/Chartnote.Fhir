using Chartnote.Fhir.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chartnote.Fhir.Api.Endpoints;

[ApiController]
[Route("api/[controller]")]
public class PatientController : ControllerBase
{
    private readonly FhirService _fhirService;
    private readonly ILogger<PatientController> _logger;

    public PatientController(FhirService fhirService, ILogger<PatientController> logger)
    {
        _fhirService = fhirService;
        _logger      = logger;
    }

    [HttpGet("{teamId}/{patientId}")]
    public async Task<IActionResult> GetPatient(
        string teamId,
        string patientId,
        CancellationToken ct)
    {
        var patient = await _fhirService.GetPatientAsync(teamId, patientId, ct);
        if (patient is null) return NotFound();
        return Ok(patient);
    }
}