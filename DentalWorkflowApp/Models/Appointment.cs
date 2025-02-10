namespace DentalWorkflowApp.Models
{
    public record Appointment(
        string Id,
        string PatientId,
        DateTime DateTime,
        string ProcedureType,
        string DentistId,
        AppointmentStatus Status
    );
}