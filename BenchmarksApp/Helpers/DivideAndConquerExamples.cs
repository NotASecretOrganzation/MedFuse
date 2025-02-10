using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;

public class DivideAndConquerExamples
{
    // Threshold for parallel processing
    private const int PARALLEL_THRESHOLD = 1000;

    #region Task-based implementation
    public static async Task<int> SumArrayTaskBasedAsync(int[] array)
    {
        if (array.Length <= PARALLEL_THRESHOLD)
        {
            return array.Sum();
        }

        int mid = array.Length / 2;
        var left = array.Take(mid).ToArray();
        var right = array.Skip(mid).ToArray();

        // Create two tasks for parallel processing
        var leftTask = Task.Run(() => SumArrayTaskBasedAsync(left));
        var rightTask = Task.Run(() => SumArrayTaskBasedAsync(right));

        // Wait for both tasks and combine results
        var results = await Task.WhenAll(leftTask, rightTask);
        return results[0] + results[1];
    }
    #endregion

    #region Parallel.ForEach implementation
    public static int SumArrayParallelForEach(int[] array)
    {
        if (array.Length <= PARALLEL_THRESHOLD)
        {
            return array.Sum();
        }

        var partitioner = Partitioner.Create(0, array.Length);
        var sum = 0;
        
        Parallel.ForEach(
            partitioner,
            () => 0, // Thread local initial state
            (range, _, threadLocalSum) =>
            {
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    threadLocalSum += array[i];
                }
                return threadLocalSum;
            },
            threadLocalSum =>
            {
                Interlocked.Add(ref sum, threadLocalSum);
            }
        );

        return sum;
    }
    #endregion

    #region PLINQ implementation
    public static int SumArrayPLINQ(int[] array)
    {
        if (array.Length <= PARALLEL_THRESHOLD)
        {
            return array.Sum();
        }

        return array.AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .WithExecutionMode(ParallelExecutionMode.ForceParallelism)
            .Sum();
    }
    #endregion
    
    #region TPL Dataflow implementation
    public static async Task<int> SumArrayDataflowAsync(int[] array)
    {
        const int PARALLEL_THRESHOLD = 1000;

        if (array.Length <= PARALLEL_THRESHOLD)
        {
            return array.Sum();
        }

        var options = new ExecutionDataflowBlockOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        // Create blocks for the pipeline
        var splitBlock = new TransformManyBlock<int[], int[]>(
            input =>
            {
                // Split array into chunks
                return Enumerable.Range(0, Environment.ProcessorCount)
                    .Select(i => input.Skip(i * (input.Length / Environment.ProcessorCount))
                                    .Take(input.Length / Environment.ProcessorCount)
                                    .ToArray());
            }, options);

        var sumBlock = new TransformBlock<int[], int>(
            chunk => chunk.Sum(),
            options);

        int totalSum = 0;

        var aggregateBlock = new ActionBlock<int>(
            value => Interlocked.Add(ref totalSum, value),
            options);


        // Link the blocks
        var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
        splitBlock.LinkTo(sumBlock, linkOptions);
        sumBlock.LinkTo(aggregateBlock, linkOptions);

        // Start the pipeline
        await splitBlock.SendAsync(array);
        splitBlock.Complete();
        await aggregateBlock.Completion;

        return totalSum;
    }
    #endregion

    #region Custom recursive parallel implementation
    public static async Task<int> SumArrayCustomParallelAsync(int[] array, int depth = 0)
    {
        // Base case: small enough array or too deep recursion
        if (array.Length <= PARALLEL_THRESHOLD || depth >= Math.Log(Environment.ProcessorCount, 2))
        {
            return array.Sum();
        }

        // Split array into chunks based on processor count
        int chunkSize = array.Length / Environment.ProcessorCount;
        var tasks = new List<Task<int>>();

        for (int i = 0; i < Environment.ProcessorCount; i++)
        {
            var chunk = array.Skip(i * chunkSize)
                           .Take(chunkSize)
                           .ToArray();
            
            tasks.Add(Task.Run(() => SumArrayCustomParallelAsync(chunk, depth + 1)));
        }

        var results = await Task.WhenAll(tasks);
        return results.Sum();
    }
    #endregion

    #region Benchmarking helper
    public static async Task RunBenchmarksAsync()
    {
        // Generate test data
        var random = new Random(42);
        var array = Enumerable.Range(0, 10_000_000)
                            .Select(_ => random.Next(1, 100))
                            .ToArray();

        // Warm up
        await SumArrayTaskBasedAsync(array[..1000]);
        SumArrayParallelForEach(array[..1000]);
        SumArrayPLINQ(array[..1000]);
        await SumArrayDataflowAsync(array[..1000]);
        await SumArrayCustomParallelAsync(array[..1000]);

        // Benchmark each implementation
        var implementations = new Dictionary<string, Func<Task<int>>>
        {
            ["Task-based"] = () => SumArrayTaskBasedAsync(array),
            ["Parallel.ForEach"] = () => Task.FromResult(SumArrayParallelForEach(array)),
            ["PLINQ"] = () => Task.FromResult(SumArrayPLINQ(array)),
            ["TPL Dataflow"] = () => SumArrayDataflowAsync(array),
            ["Custom Parallel"] = () => SumArrayCustomParallelAsync(array)
        };

        foreach (var (name, implementation) in implementations)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = await implementation();
            sw.Stop();

            Console.WriteLine($"{name,-15} | Time: {sw.ElapsedMilliseconds,6}ms | Result: {result}");
        }
    }
    #endregion
}