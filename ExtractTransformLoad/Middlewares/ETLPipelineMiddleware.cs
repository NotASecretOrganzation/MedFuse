// Middleware Components
namespace ExtractTransformLoad.Middlewares
{
    public class ETLPipelineMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ETLPipelineMiddleware> _logger;

        public ETLPipelineMiddleware(RequestDelegate next, ILogger<ETLPipelineMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var etlContext = context.RequestServices.GetRequiredService<ETLContext>();

            context.Response.OnCompleted(async () =>
            {
                _logger.LogInformation("ETL Pipeline completed for job {JobId}", etlContext.JobId);
                await CleanupAsync(etlContext);
            });

            await _next(context);
        }

        private async Task CleanupAsync(ETLContext context)
        {
            context.DataChannel.Writer.Complete();
            await Task.Delay(100); // Allow final processing
        }
    }
}