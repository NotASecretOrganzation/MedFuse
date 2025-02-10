using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;

namespace ParallelProcessingDemo
{

    // Sample data processing function
    public class ParallelProcessingDemo
    {

        private static async Task<string> ProcessItem(int item)
        {
            await Task.Delay(100); // Simulate work
            return $"Processed {item}";
        }

        public static async Task TPLExample()
        {
            var items = Enumerable.Range(1, 100).ToList();

            // Method 1: Parallel.ForEach
            Console.WriteLine("Using Parallel.ForEach:");
            Parallel.ForEach(
                items,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                item => Console.WriteLine($"Parallel processing item {item}")
            );

            // Method 2: Task.WhenAll with PLINQ
            Console.WriteLine("\nUsing PLINQ:");
            var plinqResults = items.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .Select(async item => await ProcessItem(item))
                .ToList();

            await Task.WhenAll(plinqResults);

            // Method 3: Custom partitioning with Tasks
            Console.WriteLine("\nUsing custom task partitioning:");
            var batches = items
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index % Environment.ProcessorCount)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

            var batchTasks = batches.Select(async batch =>
            {
                foreach (var item in batch)
                {
                    await ProcessItem(item);
                }
            });

            await Task.WhenAll(batchTasks);
        }

        public static void RxExample()
        {
            var items = Enumerable.Range(1, 100);

            // Method 1: Basic parallel processing with Rx
            Console.WriteLine("Using Rx parallel processing:");
            Observable.FromAsync(() => Task.FromResult(items))
                .SelectMany(x => x)
                .Select(x => Observable.FromAsync(() => ProcessItem(x)))
                .Merge(Environment.ProcessorCount)
                .Subscribe(
                    result => Console.WriteLine(result),
                    error => Console.WriteLine($"Error: {error}"),
                    () => Console.WriteLine("Processing completed")
                );

            // Method 2: Batched processing with Rx
            Console.WriteLine("\nUsing Rx batched processing:");
            Observable.FromAsync(() => Task.FromResult(items))
                .SelectMany(x => x)
                .Buffer(10) // Process in batches of 10
                .Select(batch => Observable.FromAsync(async () =>
                {
                    var tasks = batch.Select(ProcessItem);
                    return await Task.WhenAll(tasks);
                }))
                .Merge(Environment.ProcessorCount)
                .Subscribe(
                    results => Console.WriteLine($"Processed batch of {results.Length} items"),
                    error => Console.WriteLine($"Error: {error}"),
                    () => Console.WriteLine("Batch processing completed")
                );
        }

        public static async Task RunDemo()
        {
            // Run TPL examples
            await TPLExample();

            // Run Rx examples
            RxExample();

            // Wait a bit to see Rx results (since they're async)
            await Task.Delay(2000);
        }
    }
}