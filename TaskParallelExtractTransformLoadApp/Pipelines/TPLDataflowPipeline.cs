using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;
using TaskParallelExtractTransform_LoadApp.Interfaces;
using TaskParallelExtractTransform_LoadApp.Models;
using TaskParallelExtractTransformLoadApp.Models;

public class TPLDataflowPipeline : IETLPipeline
{
    private readonly ILogger<TPLDataflowPipeline> _logger;
    private readonly IDataSource _dataSource;
    private readonly IDataTransformer _transformer;
    private readonly IDataLoader _loader;
    private readonly ETLStats _stats;
    private readonly ExecutionDataflowBlockOptions _defaultOptions;

    public TPLDataflowPipeline(
        ILogger<TPLDataflowPipeline> logger,
        IDataSource dataSource,
        IDataTransformer transformer,
        IDataLoader loader)
    {
        _logger = logger;
        _dataSource = dataSource;
        _transformer = transformer;
        _loader = loader;
        _stats = new ETLStats();
        
        _defaultOptions = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            BoundedCapacity = 1000,
            CancellationToken = CancellationToken.None
        };
    }

    public async Task<ETLStats> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _defaultOptions.CancellationToken = cancellationToken;
            var pipeline = CreatePipeline();
            
            await foreach (var batch in _dataSource.ExtractBatchesAsync(cancellationToken))
            {
                await pipeline.ExtractBlock.SendAsync(batch, cancellationToken);
                _stats.ExtractedCount += batch.Count;
            }

            pipeline.ExtractBlock.Complete();
            await pipeline.LoadBlock.Completion;
            
            _stats.Status = "Completed";
            return _stats;
        }
        catch (OperationCanceledException)
        {
            _stats.Status = "Cancelled";
            throw;
        }
        catch (Exception ex)
        {
            _stats.Status = "Failed";
            _logger.LogError(ex, "Pipeline execution failed");
            throw;
        }
    }

    private (TransformBlock<List<RawData>, List<TransformedData>> ExtractBlock,
             TransformBlock<List<TransformedData>, List<TransformedData>> TransformBlock,
             ActionBlock<List<TransformedData>> LoadBlock) CreatePipeline()
    {
        // Extract Block: Processes raw data in batches
        var extractBlock = new TransformBlock<List<RawData>, List<TransformedData>>(
            async batch =>
            {
                try
                {
                    var results = new List<TransformedData>();
                    await Parallel.ForEachAsync(batch, _defaultOptions.CancellationToken, 
                        async (item, ct) =>
                        {
                            var result = await _transformer.PreProcessAsync(item, ct);
                            lock(results) results.Add(result);
                        });
                    var extractedCount = _stats.ExtractedCount;
                    Interlocked.Add(ref extractedCount, batch.Count);
                    _stats.ExtractedCount = extractedCount;
                    return results;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Extraction error for batch");
                    var errorCount = _stats.ErrorCount;
                    Interlocked.Increment(ref errorCount);
                    _stats.ErrorCount = errorCount;
                    throw;
                }
            }, _defaultOptions);

        // Transform Block: Applies business logic transformations
        var transformBlock = new TransformBlock<List<TransformedData>, List<TransformedData>>(
            async batch =>
            {
                try
                {
                    var partitioner = Partitioner.Create(batch, true);
                    var results = new ConcurrentBag<TransformedData>();
                    
                    await Parallel.ForEachAsync(partitioner.GetDynamicPartitions(), _defaultOptions.CancellationToken,
                        async (item, ct) =>
                        {
                            var transformed = await _transformer.TransformAsync(item, ct);
                            results.Add(transformed);
                            var transformedCount = _stats.TransformedCount;
                            Interlocked.Increment(ref transformedCount);
                            _stats.TransformedCount = transformedCount;
                        });
                    
                    return results.ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Transformation error for batch");
                    var errorCount = _stats.ErrorCount;
                    Interlocked.Increment(ref errorCount);
                    _stats.ErrorCount = errorCount;
                    throw;
                }
            }, _defaultOptions);

        // Load Block: Loads transformed data into the destination
        var loadBlock = new ActionBlock<List<TransformedData>>(
            async batch =>
            {
                try
                {
                    await _loader.LoadBatchAsync(batch, _defaultOptions.CancellationToken);
                    var loadedCount = _stats.LoadedCount;
                    Interlocked.Add(ref loadedCount, batch.Count);
                    _stats.LoadedCount = loadedCount;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Loading error for batch");
                    var errorCount = _stats.ErrorCount;
                    Interlocked.Increment(ref errorCount);
                    _stats.ErrorCount = errorCount;
                    throw;
                }
            }, _defaultOptions);

        // Link the blocks
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

        extractBlock.LinkTo(transformBlock, linkOptions);
        transformBlock.LinkTo(loadBlock, linkOptions);

        return (extractBlock, transformBlock, loadBlock);
    }

    Task<ETLStats> IETLPipeline.ExecuteAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}