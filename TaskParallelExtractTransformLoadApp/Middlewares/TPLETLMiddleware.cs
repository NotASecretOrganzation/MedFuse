// Middleware for pipeline execution and monitoring
using System.Collections.Concurrent;
using TaskParallelExtractTransform_LoadApp.Interfaces;
using TaskParallelExtractTransform_LoadApp.Models;
using TaskParallelExtractTransformLoadApp.Models;

namespace TaskParallelExtractTransform_LoadApp.Middlewares
{
    public class TPLETLMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TPLETLMiddleware> _logger;
        private readonly ConcurrentDictionary<string, ETLStats> _activeJobs;

        public TPLETLMiddleware(
            RequestDelegate next,
            ILogger<TPLETLMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _activeJobs = new ConcurrentDictionary<string, ETLStats>();
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/etl/start")
            {
                var jobId = Guid.NewGuid().ToString();
                var pipeline = context.RequestServices.GetRequiredService<IETLPipeline>();
                var cts = new CancellationTokenSource();

                // Start pipeline execution
                var execution = Task.Run(async () =>
                {
                    try
                    {
                        var stats = await pipeline.ExecuteAsync(cts.Token);
                        _activeJobs.TryAdd(jobId, stats);
                        return stats;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Job {JobId} failed", jobId);
                        throw;
                    }
                }, cts.Token);

                await context.Response.WriteAsJsonAsync(new { JobId = jobId });
                return;
            }

            if (context.Request.Path == "/etl/status")
            {
                var jobId = context.Request.Query["jobId"].ToString();
                if (_activeJobs.TryGetValue(jobId, out var stats))
                {
                    await context.Response.WriteAsJsonAsync(stats);
                    return;
                }

                context.Response.StatusCode = 404;
                return;
            }

            await _next(context);
        }
    }
}