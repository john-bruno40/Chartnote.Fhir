using Chartnote.Fhir.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chartnote.Fhir.Api.Endpoints;

[ApiController]
[Route("api/[controller]")]
public class PractitionerController : ControllerBase
{
    private readonly FhirService _fhirService;

    public PractitionerController(FhirService fhirService)
    {
        _fhirService = fhirService;
    }

    [HttpGet("{teamId}/search")]
    public async Task<IActionResult> SearchPractitioners(
        string teamId,
        [FromQuery] string? family,
        [FromQuery] string? given,
        CancellationToken ct)
    {
        var practitioners = await _fhirService.SearchPractitionersAsync(
            teamId, family, given, ct);

        if (practitioners.Count == 0) return NotFound("No practitioners found.");
        return Ok(practitioners);
    }
}