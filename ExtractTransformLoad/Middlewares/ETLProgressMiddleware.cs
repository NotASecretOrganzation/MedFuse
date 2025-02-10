// Progress Monitoring Middleware
using ExtractTransformLoad.Models;

namespace ExtractTransformLoad.Middlewares
{
    public class ETLProgressMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ETLProgressMiddleware> _logger;

        public ETLProgressMiddleware(RequestDelegate next, ILogger<ETLProgressMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ETLContext etlContext)
        {
            if (context.Request.Path == "/etl-progress")
            {
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.Add("Cache-Control", "no-cache");
                context.Response.Headers.Add("Connection", "keep-alive");

                while (!context.RequestAborted.IsCancellationRequested)
                {
                    var progress = new ETLProgress
                    {
                        JobId = etlContext.JobId,
                        Stage = "Processing",
                        ProcessedItems = 0, // Update with real metrics
                        TotalItems = 0,
                        Status = "Running"
                    };

                    await context.Response.WriteAsync($"data: {System.Text.Json.JsonSerializer.Serialize(progress)}\n\n");
                    await context.Response.Body.FlushAsync();
                    await Task.Delay(1000);
                }
            }
            else
            {
                await _next(context);
            }
        }
    }
}