namespace Chartnote.Fhir.Api.Models;

public class ScheduleEntry
{
    public string?           AppointmentFhirId { get; set; }
    public DateTime          Start             { get; set; }
    public DateTime?         End               { get; set; }
    public string?           Status            { get; set; }
    public string?           AppointmentType   { get; set; }
    public ChartnotePatient? Patient           { get; set; }
    public string?           EhrSource         { get; set; }  // "Epic" | "Altera"
}