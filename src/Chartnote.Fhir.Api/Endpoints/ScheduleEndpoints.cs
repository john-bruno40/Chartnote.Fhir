using Chartnote.Fhir.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Chartnote.Fhir.Api.Endpoints;

[ApiController]
[Route("api/[controller]")]
public class ScheduleController : ControllerBase
{
    private readonly FhirService _fhirService;
    private readonly ILogger<ScheduleController> _logger;

    public ScheduleController(FhirService fhirService, ILogger<ScheduleController> logger)
    {
        _fhirService = fhirService;
        _logger      = logger;
    }

    [HttpGet("{teamId}/{patientId}")]
    [HttpGet("{teamId}/{patientId}/{date}")]
    public async Task<IActionResult> GetSchedule(
        string teamId,
        string patientId,
        DateOnly? date = null,
        CancellationToken ct = default)
    {
        var schedule = await _fhirService.GetDailyScheduleAsync(
            teamId, patientId, date, ct);

        if (schedule.Count == 0) return NotFound("No appointments found.");
        return Ok(schedule);
    }
}