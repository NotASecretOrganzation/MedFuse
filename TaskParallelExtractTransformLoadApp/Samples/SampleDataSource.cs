using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskParallelExtractTransform_LoadApp.Interfaces;
using TaskParallelExtractTransform_LoadApp.Models;

namespace TaskParallelExtractTransform_LoadApp.Samples
{
    public class SampleDataSource : IDataSource
    {
        private readonly ILogger<SampleDataSource> _logger;
        private const int BatchSize = 100;
        private const int TotalItems = 1000;

        public SampleDataSource(ILogger<SampleDataSource> logger)
        {
            _logger = logger;
        }

        public async IAsyncEnumerable<List<RawData>> ExtractBatchesAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var currentBatch = new List<RawData>();

                for (int i = 0; i < TotalItems; i++)
                {
                    // Check cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    // Simulate data retrieval
                    await Task.Delay(10, cancellationToken);

                    // Create sample data
                    var rawData = new RawData
                    {
                        Id = i.ToString(),
                        RawContent = $"Sample_{i}",
                        Metadata = new Dictionary<string, object>
                        {
                            ["timestamp"] = DateTime.UtcNow,
                            ["source"] = "SampleDataSource",
                            ["batch"] = i / BatchSize
                        }
                    };

                    currentBatch.Add(rawData);

                    // When batch is full or it's the last item, yield the batch
                    if (currentBatch.Count >= BatchSize || i == TotalItems - 1)
                    {
                        _logger.LogInformation("Yielding batch of {Count} items", currentBatch.Count);
                        yield return new List<RawData>(currentBatch);
                        currentBatch.Clear();
                    }
                }
            // try
            // {
                
            // }
            // catch (OperationCanceledException)
            // {
            //     _logger.LogInformation("Batch extraction was cancelled");
            //     throw;
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, "Error during batch extraction");
            //     throw;
            // }
        }

        public async IAsyncEnumerable<object> ExtractDataAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
             for (int i = 0; i < TotalItems; i++)
                {
                    // Check cancellation
                    cancellationToken.ThrowIfCancellationRequested();

                    // Simulate data retrieval
                    await Task.Delay(10, cancellationToken);

                    var data = new
                    {
                        Id = i,
                        Data = $"Sample_{i}",
                        Timestamp = DateTime.UtcNow,
                        Source = "SampleDataSource"
                    };

                    _logger.LogDebug("Extracted item {Id}", data.Id);
                    yield return data;
                }
            // try
            // {
            //     for (int i = 0; i < TotalItems; i++)
            //     {
            //         // Check cancellation
            //         cancellationToken.ThrowIfCancellationRequested();

            //         // Simulate data retrieval
            //         await Task.Delay(10, cancellationToken);

            //         var data = new
            //         {
            //             Id = i,
            //             Data = $"Sample_{i}",
            //             Timestamp = DateTime.UtcNow,
            //             Source = "SampleDataSource"
            //         };

            //         _logger.LogDebug("Extracted item {Id}", data.Id);
            //         yield return data;
            //     }
            // }
            // catch (OperationCanceledException)
            // {
            //     _logger.LogInformation("Data extraction was cancelled");
            //     throw;
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, "Error during data extraction");
            //     throw;
            // }
        }

        // Helper method to simulate data retrieval with error handling
        private async Task<RawData> CreateSampleDataAsync(
            int index,
            CancellationToken cancellationToken)
        {
            try
            {
                // Simulate potential failures or delays
                if (index % 100 == 0)
                {
                    await Task.Delay(100, cancellationToken); // Simulate longer processing
                }

                if (index % 500 == 0)
                {
                    throw new TimeoutException("Simulated timeout for testing");
                }

                return new RawData
                {
                    Id = index.ToString(),
                    RawContent = $"Sample_{index}",
                    Metadata = new Dictionary<string, object>
                    {
                        ["timestamp"] = DateTime.UtcNow,
                        ["source"] = "SampleDataSource",
                        ["isRetry"] = false
                    }
                };
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Error creating sample data for index {Index}", index);
                
                // Return fallback data on error
                return new RawData
                {
                    Id = index.ToString(),
                    RawContent = $"Error_{index}",
                    Metadata = new Dictionary<string, object>
                    {
                        ["error"] = ex.Message,
                        ["timestamp"] = DateTime.UtcNow,
                        ["isError"] = true
                    }
                };
            }
        }
    }
}