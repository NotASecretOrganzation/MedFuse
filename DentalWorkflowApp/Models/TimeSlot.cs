namespace DentalWorkflowApp.Models
{
    public record TimeSlot(DateTime Start, DateTime End, bool IsAvailable);
}
