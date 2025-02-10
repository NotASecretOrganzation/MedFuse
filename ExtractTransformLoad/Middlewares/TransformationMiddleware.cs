using System.Threading.Channels;
using System.Threading.Tasks.Dataflow;

namespace ExtractTransformLoad.Middlewares
{
    public class TransformationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<TransformationMiddleware> _logger;

        public TransformationMiddleware(RequestDelegate next, ILogger<TransformationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, ETLContext etlContext)
        {
            var transformBlock = new TransformBlock<object, object>(
                async data =>
                {
                    try
                    {
                        // Apply transformations
                        await Task.Delay(10); // Simulate work
                        return data;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Transform error for item");
                        return null;
                    }
                },
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount,
                    BoundedCapacity = 100
                });

            var loadChannel = Channel.CreateBounded<object>(100);

            // Start transformation in background
            _ = Task.Run(async () =>
            {
                try
                {
                    await foreach (var item in etlContext.DataChannel.Reader.ReadAllAsync())
                    {
                        await transformBlock.SendAsync(item);
                    }
                    transformBlock.Complete();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during transformation");
                    transformBlock.Complete();
                }
            });

            // Store transformed channel for next middleware
            etlContext.Metadata["LoadChannel"] = loadChannel;

            await _next(context);
        }
    }
}