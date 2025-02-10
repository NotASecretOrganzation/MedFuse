// Middleware for workflow management
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using DentalWorkflowApp.Models;
using DentalWorkflowApp.Pipelines;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

public class DentalWorkflowMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DentalWorkflowMiddleware> _logger;
    private readonly DentalWorkflowPipeline _pipeline;
    private readonly ConcurrentDictionary<string, ITargetBlock<Appointment>> _activeWorkflows;

    public DentalWorkflowMiddleware(
        RequestDelegate next,
        ILogger<DentalWorkflowMiddleware> logger,
        DentalWorkflowPipeline pipeline)
    {
        _next = next;
        _logger = logger;
        _pipeline = pipeline;
        _activeWorkflows = new ConcurrentDictionary<string, ITargetBlock<Appointment>>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path == "/dental/checkin")
        {
            var appointment = await context.Request.ReadFromJsonAsync<Appointment>();
            var workflow = _pipeline.CreateWorkflow();
            _activeWorkflows.TryAdd(appointment.Id, workflow);

            await workflow.SendAsync(appointment);
            await context.Response.WriteAsJsonAsync(new { AppointmentId = appointment.Id });
            return;
        }

        if (context.Request.Path == "/dental/status")
        {
            var appointmentId = context.Request.Query["appointmentId"].ToString();
            // Return workflow status
            return;
        }

        await _next(context);
    }
}