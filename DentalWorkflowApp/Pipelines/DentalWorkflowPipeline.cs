using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using DentalWorkflowApp.Models;
using Microsoft.Extensions.Logging;

namespace DentalWorkflowApp.Pipelines
{
    public class DentalWorkflowPipeline
    {
        private readonly ExecutionDataflowBlockOptions _defaultOptions;
        private readonly ILogger<DentalWorkflowPipeline> _logger;
        private readonly ConcurrentDictionary<string, PatientWorkflowState> _workflowStates;

        public DentalWorkflowPipeline(ILogger<DentalWorkflowPipeline> logger)
        {
            _logger = logger;
            _workflowStates = new ConcurrentDictionary<string, PatientWorkflowState>();
            _defaultOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = 100
            };
        }

        public ITargetBlock<Appointment> CreateWorkflow()
        {
            // Check-in block
            var checkInBlock = new TransformBlock<Appointment, PatientWorkflowState>(
                async appointment =>
                {
                    var state = await ProcessCheckIn(appointment);
                    _workflowStates.TryAdd(appointment.Id, state);
                    return state;
                }, _defaultOptions);

            // Insurance verification block
            var insuranceBlock = new TransformBlock<PatientWorkflowState, PatientWorkflowState>(
                async state =>
                {
                    await VerifyInsurance(state);
                    return state;
                }, _defaultOptions);

            // Initial assessment block
            var assessmentBlock = new TransformBlock<PatientWorkflowState, PatientWorkflowState>(
                async state =>
                {
                    await PerformInitialAssessment(state);
                    return state;
                }, _defaultOptions);

            // Treatment block
            var treatmentBlock = new TransformBlock<PatientWorkflowState, PatientWorkflowState>(
                async state =>
                {
                    await PerformTreatment(state);
                    return state;
                }, _defaultOptions);

            // Follow-up block
            var followUpBlock = new ActionBlock<PatientWorkflowState>(
                async state =>
                {
                    await ScheduleFollowUp(state);
                }, _defaultOptions);

            // Link the blocks
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            checkInBlock.LinkTo(insuranceBlock, linkOptions,
                state => state.Status == WorkflowStatus.CheckedIn);

            insuranceBlock.LinkTo(assessmentBlock, linkOptions,
                state => state.Status == WorkflowStatus.InsuranceVerified);

            assessmentBlock.LinkTo(treatmentBlock, linkOptions,
                state => state.Status == WorkflowStatus.AssessmentCompleted);

            treatmentBlock.LinkTo(followUpBlock, linkOptions,
                state => state.Status == WorkflowStatus.TreatmentCompleted);

            return checkInBlock;
        }

        private async Task<PatientWorkflowState> ProcessCheckIn(Appointment appointment)
        {
            await Task.Delay(100); // Simulate check-in process
            return new PatientWorkflowState
            {
                AppointmentId = appointment.Id,
                PatientId = appointment.PatientId,
                Status = WorkflowStatus.CheckedIn,
                CheckInTime = DateTime.UtcNow
            };
        }

        private async Task VerifyInsurance(PatientWorkflowState state)
        {
            await Task.Delay(200); // Simulate insurance verification
            state.Status = WorkflowStatus.InsuranceVerified;
            state.InsuranceVerifiedAt = DateTime.UtcNow;
        }

        private async Task PerformInitialAssessment(PatientWorkflowState state)
        {
            await Task.Delay(300); // Simulate initial assessment
            state.Status = WorkflowStatus.AssessmentCompleted;
            state.AssessmentCompletedAt = DateTime.UtcNow;
        }

        private async Task PerformTreatment(PatientWorkflowState state)
        {
            await Task.Delay(500); // Simulate treatment
            state.Status = WorkflowStatus.TreatmentCompleted;
            state.TreatmentCompletedAt = DateTime.UtcNow;
        }

        private async Task ScheduleFollowUp(PatientWorkflowState state)
        {
            await Task.Delay(100); // Simulate follow-up scheduling
            state.Status = WorkflowStatus.FollowUpScheduled;
            state.FollowUpScheduledAt = DateTime.UtcNow;
        }
    }
}