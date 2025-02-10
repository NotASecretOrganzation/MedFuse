using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;
using ExtractTransformLoad.Interfaces;

namespace ExtractTransformLoad.Middlewares
{
    public class LoadingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<LoadingMiddleware> _logger;

        public LoadingMiddleware(RequestDelegate next, ILogger<LoadingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ETLContext etlContext)
        {
            var loadChannel = (Channel<object>)etlContext.Metadata["LoadChannel"];
            var dataStore = context.RequestServices.GetRequiredService<IDataStore>();

            var loadBlock = new ActionBlock<object>(
                async data =>
                {
                    try
                    {
                        await dataStore.SaveAsync(data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Load error for item");
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    BoundedCapacity = 100
                });

            // Start loading in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in loadChannel.Reader.ReadAllAsync())
                    {
                        await loadBlock.SendAsync(item);
                    }
                    loadBlock.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during loading");
                    loadBlock.Complete();
                }
            });

            await _next(context);
        }
    }
}