namespace DentalWorkflowApp.Models
{
    public record Treatment(
        string Id,
        string PatientId,
        string AppointmentId,
        string Procedure,
        string Notes,
        DateTime CompletedAt
    );
}