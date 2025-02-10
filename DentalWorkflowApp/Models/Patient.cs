namespace DentalWorkflowApp.Models
{
    public record Patient(
        string Id,
        string Name,
        DateTime DateOfBirth,
        string InsuranceInfo,
        List<string> MedicalHistory
    );
}