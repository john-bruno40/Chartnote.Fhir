using Chartnote.Fhir.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chartnote.Fhir.Api.Endpoints;

[ApiController]
[Route("api/[controller]")]
public class EncounterController : ControllerBase
{
    private readonly FhirService _fhirService;

    public EncounterController(FhirService fhirService)
    {
        _fhirService = fhirService;
    }

    [HttpGet("{teamId}/{patientId}")]
    public async Task<IActionResult> GetEncounters(
        string teamId,
        string patientId,
        CancellationToken ct)
    {
        var encounters = await _fhirService.SearchEncountersAsync(
            teamId, patientId, ct);

        if (encounters.Count == 0) 
            return NotFound(new { message = "No encounters found." });
        return Ok(encounters);
    }
}