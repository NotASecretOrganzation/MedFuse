using ExtractTransformLoad.Interfaces;
using ExtractTransformLoad.Models;

namespace ExtractTransformLoad.Middlewares
{
    public class ExtractionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExtractionMiddleware> _logger;

        public ExtractionMiddleware(RequestDelegate next, ILogger<ExtractionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ETLContext etlContext)
        {
            var dataSource = context.RequestServices.GetRequiredService<IDataSource>();
            var progress = new Progress<ETLProgress>(p =>
                _logger.LogInformation("Extraction progress: {ProcessedItems}/{TotalItems}",
                    p.ProcessedItems, p.TotalItems));

            // Start extraction in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in dataSource.ExtractDataAsync())
                    {
                        await etlContext.DataChannel.Writer.WriteAsync(item);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during extraction");
                    etlContext.DataChannel.Writer.Complete(ex);
                }
            });

            await _next(context);
        }
    }
}