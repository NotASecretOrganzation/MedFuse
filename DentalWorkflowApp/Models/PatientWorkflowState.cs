namespace DentalWorkflowApp.Models
{
    public class PatientWorkflowState
    {
        public string AppointmentId { get; set; }
        public string PatientId { get; set; }
        public WorkflowStatus Status { get; set; }
        public DateTime CheckInTime { get; set; }
        public DateTime? InsuranceVerifiedAt { get; set; }
        public DateTime? AssessmentCompletedAt { get; set; }
        public DateTime? TreatmentCompletedAt { get; set; }
        public DateTime? FollowUpScheduledAt { get; set; }
        public List<string> Notes { get; set; } = new();
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}