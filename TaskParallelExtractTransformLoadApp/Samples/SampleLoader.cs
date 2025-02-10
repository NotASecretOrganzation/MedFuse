using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using TaskParallelExtractTransform_LoadApp.Models;
using TaskParallelExtractTransform_LoadApp.Interfaces;

namespace TaskParallelExtractTransform_LoadApp.Samples
{
    public class SampleLoader : IDataLoader
    {
        private readonly ILogger<SampleLoader> _logger;
        private readonly ExecutionDataflowBlockOptions _defaultOptions;
        private readonly ConcurrentDictionary<string, LoadResult> _loadHistory;
        private readonly ConcurrentDictionary<string, object> _storage; // New storage dictionary to hold input data
        private readonly SemaphoreSlim _loadThrottle;

        public SampleLoader(ILogger<SampleLoader> logger)
        {
            _logger = logger;
            _loadHistory = new ConcurrentDictionary<string, LoadResult>();
            _storage = new ConcurrentDictionary<string, object>(); // Initialize storage
            _loadThrottle = new SemaphoreSlim(Environment.ProcessorCount * 2);
            _defaultOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = 1000
            };
        }

        public async Task<LoadResult> LoadAsync(TransformationResult data, CancellationToken cancellationToken)
        {
            try
            {
                var pipeline = CreateLoadPipeline();
                await pipeline.PrepareBlock.SendAsync(data, cancellationToken);
                pipeline.PrepareBlock.Complete();
                return await pipeline.FinalizeBlock.ReceiveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data");
                throw;
            }
        }

        public async Task LoadBatchAsync(List<TransformedData> batch, CancellationToken cancellationToken)
        {
            try
            {
                var batchPipeline = CreateBatchLoadPipeline();
                foreach (var item in batch)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    await batchPipeline.PrepareBlock.SendAsync(item, cancellationToken);
                }
                batchPipeline.PrepareBlock.Complete();
                await batchPipeline.FinalizeBlock.Completion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch");
                throw;
            }
        }

        private (TransformBlock<TransformationResult, (TransformationResult Data, bool IsValid)> PrepareBlock,
                 TransformBlock<(TransformationResult Data, bool IsValid), LoadResult> LoadBlock,
                 TransformBlock<LoadResult, LoadResult> FinalizeBlock) CreateLoadPipeline()
        {
            var prepareBlock = new TransformBlock<TransformationResult, (TransformationResult Data, bool IsValid)>(
                async data =>
                {
                    await Task.Delay(10);
                    bool isValid = data.ValidationErrors.Count == 0;
                    return (data, isValid);
                }, _defaultOptions);

            var loadBlock = new TransformBlock<(TransformationResult Data, bool IsValid), LoadResult>(
                async input =>
                {
                    var (data, isValid) = input;
                    try
                    {
                        await _loadThrottle.WaitAsync();
                        if (!isValid)
                        {
                            return new LoadResult
                            {
                                TransformationId = data.Id,
                                Success = false,
                                ErrorMessage = "Validation errors present"
                            };
                        }
                        string storageResult = data.TransformedData switch
                        {
                            string s => await StoreString(s),
                            int n => await StoreNumber(n),
                            DateTime dt => await StoreDateTime(dt),
                            _ => await StoreDefault(data.TransformedData)
                        };
                        var result = new LoadResult
                        {
                            TransformationId = data.Id,
                            Success = true,
                            StorageLocation = storageResult
                        };
                        _loadHistory.TryAdd(result.Id, result);
                        return result;
                    }
                    finally
                    {
                        _loadThrottle.Release();
                    }
                }, _defaultOptions);

            var finalizeBlock = new TransformBlock<LoadResult, LoadResult>(
                async result =>
                {
                    await Task.Delay(10);
                    if (!result.Success)
                    {
                        _logger.LogWarning("Load failed for ID: {Id}", result.Id);
                    }
                    return result;
                }, _defaultOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            prepareBlock.LinkTo(loadBlock, linkOptions);
            loadBlock.LinkTo(finalizeBlock, linkOptions);

            return (prepareBlock, loadBlock, finalizeBlock);
        }

        private (TransformBlock<TransformedData, (TransformedData Data, bool IsValid)> PrepareBlock,
                 TransformBlock<(TransformedData Data, bool IsValid), bool> LoadBlock,
                 TransformBlock<bool, bool> FinalizeBlock) CreateBatchLoadPipeline()
        {
            var prepareBlock = new TransformBlock<TransformedData, (TransformedData Data, bool IsValid)>(
                async data =>
                {
                    await Task.Delay(10);
                    return (data, true);
                }, _defaultOptions);

            var loadBlock = new TransformBlock<(TransformedData Data, bool IsValid), bool>(
                async input =>
                {
                    try
                    {
                        await _loadThrottle.WaitAsync();
                        var (data, isValid) = input;
                        if (isValid)
                        {
                            await StoreDefault(data);
                        }
                        return true;
                    }
                    finally
                    {
                        _loadThrottle.Release();
                    }
                }, _defaultOptions);

            var finalizeBlock = new TransformBlock<bool, bool>(
                async success =>
                {
                    await Task.Delay(10);
                    if (!success)
                    {
                        _logger.LogWarning("Batch processing encountered issues");
                    }
                    return success;
                }, _defaultOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            prepareBlock.LinkTo(loadBlock, linkOptions);
            loadBlock.LinkTo(finalizeBlock, linkOptions);

            return (prepareBlock, loadBlock, finalizeBlock);
        }

        // Updated store methods that now store the input in _storage dictionary before returning a key.
        private async Task<string> StoreString(string data)
        {
            await Task.Delay(50);
            var key = $"string_store/{Guid.NewGuid()}";
            _storage.TryAdd(key, data);
            return key;
        }

        private async Task<string> StoreNumber(int data)
        {
            await Task.Delay(50);
            var key = $"number_store/{Guid.NewGuid()}";
            _storage.TryAdd(key, data);
            return key;
        }

        private async Task<string> StoreDateTime(DateTime data)
        {
            await Task.Delay(50);
            var key = $"datetime_store/{Guid.NewGuid()}";
            _storage.TryAdd(key, data);
            return key;
        }

        private async Task<string> StoreDefault(object data)
        {
            await Task.Delay(50);
            var key = $"default_store/{Guid.NewGuid()}";
            _storage.TryAdd(key, data);
            return key;
        }
    }
}
