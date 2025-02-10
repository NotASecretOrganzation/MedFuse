using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;
using TaskParallelExtractTransform_LoadApp.Interfaces;
using TaskParallelExtractTransform_LoadApp.Models;
using TaskParallelExtractTransformLoadApp.Models;

namespace TaskParallelExtractTransformLoadApp.Samples
{
    public class SampleTransformer : IDataTransformer
    {
        private readonly ILogger<SampleTransformer> _logger;
        private readonly ExecutionDataflowBlockOptions _defaultOptions;
        private readonly ConcurrentDictionary<string, TransformationResult> _transformationCache;

        public SampleTransformer(ILogger<SampleTransformer> logger)
        {
            _logger = logger;
            _transformationCache = new ConcurrentDictionary<string, TransformationResult>();
            _defaultOptions = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                BoundedCapacity = 1000
            };
        }

        public async Task<TransformationResult> TransformAsync(object data, CancellationToken cancellationToken)
        {
            try
            {
                var pipeline = CreateTransformationPipeline();
                // Send data through the pipeline
                await pipeline.ValidateBlock.SendAsync(data, cancellationToken);
                pipeline.ValidateBlock.Complete();

                // Await and return the final result from the pipeline
                return await pipeline.FinalizeBlock.ReceiveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming data");
                throw;
            }
        }

        public async Task<TransformedData> PreProcessAsync(RawData data, CancellationToken cancellationToken)
        {
            try
            {
                var pipeline = CreatePreProcessingPipeline();
                await pipeline.PreProcessBlock.SendAsync(data, cancellationToken);
                pipeline.PreProcessBlock.Complete();

                return await pipeline.FinalizeBlock.ReceiveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pre-processing: {Error}", ex.Message);
                throw;
            }
        }

        public async Task<TransformedData> TransformAsync(TransformedData data, CancellationToken cancellationToken)
        {
            try
            {
                var pipeline = CreateTransformedDataPipeline();
                await pipeline.TransformBlock.SendAsync(data, cancellationToken);
                pipeline.TransformBlock.Complete();

                return await pipeline.FinalizeBlock.ReceiveAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transforming data: {Error}", ex.Message);
                throw;
            }
        }

        private (TransformBlock<object, (object Data, List<string> Errors)> ValidateBlock,
                 TransformBlock<(object Data, List<string> Errors), TransformationResult> TransformBlock,
                 TransformBlock<TransformationResult, TransformationResult> FinalizeBlock) CreateTransformationPipeline()
        {
            // Validation block
            var validateBlock = new TransformBlock<object, (object? Data, List<string> Errors)>(
                async data =>
                {
                    var errors = new List<string>();
                    if (data == null)
                    {
                        errors.Add("Data cannot be null");
                        return (data, errors);
                    }
                    await Task.Delay(10); // Simulate validation work
                    return (data, errors);
                }, _defaultOptions);

            // Transformation block
            var transformBlock = new TransformBlock<(object? Data, List<string> Errors), TransformationResult>(
                async input =>
                {
                    var (data, errors) = input;
                    var warnings = new List<string>();
                    try
                    {
                        object? transformedData = data switch
                        {
                            string s => await TransformString(s),
                            int n => await TransformNumber(n),
                            DateTime dt => await TransformDateTime(dt),
                            _ => await TransformDefault(data)
                        };

                        var result = new TransformationResult
                        {
                            OriginalData = data,
                            TransformedData = transformedData,
                            ValidationErrors = errors,
                            Warnings = warnings
                        };
                        _transformationCache.TryAdd(result.Id, result);
                        return result;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Transformation error for data: {Data}", data);
                        errors.Add($"Transformation error: {ex.Message}");
                        return new TransformationResult
                        {
                            OriginalData = data,
                            TransformedData = data,
                            ValidationErrors = errors,
                            Warnings = warnings
                        };
                    }
                }, _defaultOptions);

            // Finalization block as a TransformBlock that passes the result along
            var finalizeBlock = new TransformBlock<TransformationResult, TransformationResult>(
                async result =>
                {
                    await Task.Delay(10); // Simulate work
                    if (result.ValidationErrors.Count > 0)
                        _logger.LogWarning("Transformation completed with errors for ID: {Id}", result.Id);
                    return result;
                }, _defaultOptions);

            // Link blocks with propagation of completion
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            validateBlock.LinkTo(transformBlock, linkOptions);
            transformBlock.LinkTo(finalizeBlock, linkOptions);

            return (validateBlock, transformBlock, finalizeBlock);
        }

        private (TransformBlock<RawData, TransformedData> PreProcessBlock,
                 TransformBlock<TransformedData, TransformedData> FinalizeBlock) CreatePreProcessingPipeline()
        {
            var preProcessBlock = new TransformBlock<RawData, TransformedData>(
                async data =>
                {
                    try
                    {
                        await Task.Delay(10); // Simulate work
                        return new TransformedData(
                            data.Id,
                            (await TransformDefault(data.RawContent))?.ToString() ?? string.Empty,
                            DateTime.UtcNow
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Pre-processing error for data ID: {Id}", data.Id);
                        throw;
                    }
                }, _defaultOptions);

            var finalizeBlock = new TransformBlock<TransformedData, TransformedData>(
                async d =>
                {
                    await Task.Delay(10); // Simulate finalization work
                    _logger.LogInformation("Pre-processing completed for ID: {Id}", d.Id);
                    return d;
                }, _defaultOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            preProcessBlock.LinkTo(finalizeBlock, linkOptions);

            return (preProcessBlock, finalizeBlock);
        }

        private (TransformBlock<TransformedData, TransformedData> TransformBlock,
                 TransformBlock<TransformedData, TransformedData> FinalizeBlock) CreateTransformedDataPipeline()
        {
            var transformBlock = new TransformBlock<TransformedData, TransformedData>(
                async data =>
                {
                    try
                    {
                        await Task.Delay(10); // Simulate work
                        object? transformedData = data.ProcessedData switch
                        {
                            string s => await TransformString(s),
                            int n => await TransformNumber(n),
                            DateTime dt => await TransformDateTime(dt),
                            _ => await TransformDefault(data.ProcessedData)
                        };
                        return data with {
                            ProcessedData = transformedData,
                            ProcessedAt = DateTime.UtcNow
                        };
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Transform error for data ID: {Id}", data.Id);
                        throw;
                    }
                }, _defaultOptions);

            var finalizeBlock = new TransformBlock<TransformedData, TransformedData>(
                async d =>
                {
                    await Task.Delay(10); // Simulate finalization work
                    _logger.LogInformation("Transformation completed for ID: {Id}", d.Id);
                    return d;
                }, _defaultOptions);

            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
            transformBlock.LinkTo(finalizeBlock, linkOptions);

            return (transformBlock, finalizeBlock);
        }

        private async Task<object> TransformString(string data)
        {
            await Task.Delay(10);
            return data.Trim().ToUpperInvariant();
        }

        private async Task<object> TransformNumber(int data)
        {
            await Task.Delay(10);
            return data * 2;
        }

        private async Task<object> TransformDateTime(DateTime data)
        {
            await Task.Delay(10);
            return data.ToUniversalTime();
        }

        private async Task<object?> TransformDefault(object? data)
        {
            await Task.Delay(10);
            return data;
        }
    }
}
