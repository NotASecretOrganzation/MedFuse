using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// HyperLogLog implementation for cardinality estimation
/// Based on the algorithm described in https://en.wikipedia.org/wiki/HyperLogLog
/// </summary>
public class HyperLogLog
{
    private readonly int _precision;
    private readonly int _numberOfBuckets;
    private readonly byte[] _buckets;
    private readonly double _alphaMM;

    /// <summary>
    /// Creates a new HyperLogLog instance
    /// </summary>
    /// <param name="precision">Precision parameter (typically 4-16). Higher values give better accuracy but use more memory.</param>
    public HyperLogLog(int precision = 14)
    {
        if (precision < 4 || precision > 16)
        {
            throw new ArgumentException("Precision must be between 4 and 16", nameof(precision));
        }

        _precision = precision;
        _numberOfBuckets = 1 << precision; // 2^precision
        _buckets = new byte[_numberOfBuckets];
        
        // Calculate alpha constant based on number of buckets
        _alphaMM = _numberOfBuckets switch
        {
            16 => 0.673,
            32 => 0.697,
            64 => 0.709,
            _ => 0.7213 / (1.0 + 1.079 / _numberOfBuckets)
        } * _numberOfBuckets * _numberOfBuckets;
    }

    /// <summary>
    /// Adds an element to the HyperLogLog
    /// </summary>
    public void Add(string element)
    {
        var hash = ComputeHash(element);
        var bucketIndex = (int)(hash & (_numberOfBuckets - 1)); // Get first 'precision' bits
        var remainingBits = hash >> _precision;
        var leadingZeros = CountLeadingZeros(remainingBits, 32 - _precision) + 1;
        
        _buckets[bucketIndex] = Math.Max(_buckets[bucketIndex], (byte)leadingZeros);
    }

    /// <summary>
    /// Estimates the cardinality (number of unique elements)
    /// </summary>
    public long EstimateCardinality()
    {
        double rawEstimate = _alphaMM / _buckets.Sum(b => Math.Pow(2, -b));

        // Apply bias correction for different ranges
        if (rawEstimate <= 2.5 * _numberOfBuckets)
        {
            // Small range correction
            int zeros = _buckets.Count(b => b == 0);
            if (zeros != 0)
            {
                rawEstimate = _numberOfBuckets * Math.Log((double)_numberOfBuckets / zeros);
            }
        }
        else if (rawEstimate > (1.0 / 30.0) * (1L << 32))
        {
            // Large range correction
            rawEstimate = -(1L << 32) * Math.Log(1 - rawEstimate / (1L << 32));
        }

        return (long)rawEstimate;
    }

    /// <summary>
    /// Merges another HyperLogLog into this one
    /// </summary>
    public void Merge(HyperLogLog other)
    {
        if (_precision != other._precision)
        {
            throw new ArgumentException("Cannot merge HyperLogLogs with different precisions");
        }

        for (int i = 0; i < _numberOfBuckets; i++)
        {
            _buckets[i] = Math.Max(_buckets[i], other._buckets[i]);
        }
    }

    /// <summary>
    /// Resets the HyperLogLog to its initial state
    /// </summary>
    public void Clear()
    {
        Array.Clear(_buckets, 0, _buckets.Length);
    }

    /// <summary>
    /// Computes a 32-bit hash of the input string
    /// </summary>
    private static uint ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return BitConverter.ToUInt32(hashBytes, 0);
    }

    /// <summary>
    /// Counts the number of leading zero bits in a value with specified bit width
    /// </summary>
    private static int CountLeadingZeros(uint value, int bitWidth)
    {
        if (value == 0) return bitWidth;
        
        // Normalize to bitWidth by shifting left
        value <<= (32 - bitWidth);
        
        int count = 0;
        if ((value & 0xFFFF0000) == 0) { count += 16; value <<= 16; }
        if ((value & 0xFF000000) == 0) { count += 8; value <<= 8; }
        if ((value & 0xF0000000) == 0) { count += 4; value <<= 4; }
        if ((value & 0xC0000000) == 0) { count += 2; value <<= 2; }
        if ((value & 0x80000000) == 0) { count += 1; }
        
        return count;
    }
}

/// <summary>
/// Examples and benchmarks for HyperLogLog
/// </summary>
public class HyperLogLogExamples
{
    /// <summary>
    /// Demonstrates basic HyperLogLog usage
    /// </summary>
    public static void BasicExample()
    {
        Console.WriteLine("=== HyperLogLog Basic Example ===\n");

        var hll = new HyperLogLog(precision: 12);
        
        // Add 10,000 unique elements
        Console.WriteLine("Adding 10,000 unique elements...");
        for (int i = 0; i < 10000; i++)
        {
            hll.Add($"user_{i}");
        }

        var estimate = hll.EstimateCardinality();
        var error = Math.Abs(10000 - estimate) / 10000.0 * 100;
        
        Console.WriteLine($"Actual count: 10,000");
        Console.WriteLine($"Estimated count: {estimate:N0}");
        Console.WriteLine($"Error: {error:F2}%\n");
    }

    /// <summary>
    /// Demonstrates HyperLogLog with duplicate elements
    /// </summary>
    public static void DuplicateExample()
    {
        Console.WriteLine("=== HyperLogLog Duplicate Handling ===\n");

        var hll = new HyperLogLog(precision: 12);
        
        // Add 100,000 elements with only 1,000 unique values
        Console.WriteLine("Adding 100,000 elements (1,000 unique)...");
        for (int i = 0; i < 100000; i++)
        {
            hll.Add($"user_{i % 1000}");
        }

        var estimate = hll.EstimateCardinality();
        var error = Math.Abs(1000 - estimate) / 1000.0 * 100;
        
        Console.WriteLine($"Actual unique count: 1,000");
        Console.WriteLine($"Estimated count: {estimate:N0}");
        Console.WriteLine($"Error: {error:F2}%\n");
    }

    /// <summary>
    /// Demonstrates merging multiple HyperLogLog instances
    /// </summary>
    public static void MergeExample()
    {
        Console.WriteLine("=== HyperLogLog Merge Example ===\n");

        var hll1 = new HyperLogLog(precision: 12);
        var hll2 = new HyperLogLog(precision: 12);
        
        // Add different elements to each HLL
        Console.WriteLine("HLL1: Adding users 0-4999");
        for (int i = 0; i < 5000; i++)
        {
            hll1.Add($"user_{i}");
        }

        Console.WriteLine("HLL2: Adding users 2500-7499 (2500 overlap)");
        for (int i = 2500; i < 7500; i++)
        {
            hll2.Add($"user_{i}");
        }

        var estimate1 = hll1.EstimateCardinality();
        var estimate2 = hll2.EstimateCardinality();
        
        Console.WriteLine($"\nHLL1 estimate: {estimate1:N0}");
        Console.WriteLine($"HLL2 estimate: {estimate2:N0}");

        // Merge HLL2 into HLL1
        hll1.Merge(hll2);
        var mergedEstimate = hll1.EstimateCardinality();
        var error = Math.Abs(7500 - mergedEstimate) / 7500.0 * 100;
        
        Console.WriteLine($"\nAfter merge:");
        Console.WriteLine($"Actual unique count: 7,500");
        Console.WriteLine($"Estimated count: {mergedEstimate:N0}");
        Console.WriteLine($"Error: {error:F2}%\n");
    }

    /// <summary>
    /// Benchmarks HyperLogLog accuracy across different precisions
    /// </summary>
    public static void PrecisionBenchmark()
    {
        Console.WriteLine("=== HyperLogLog Precision Benchmark ===\n");
        Console.WriteLine("Comparing accuracy and memory usage for different precision values\n");

        var precisions = new[] { 8, 10, 12, 14, 16 };
        const int actualCount = 100000;

        Console.WriteLine($"{"Precision",-10} {"Buckets",-10} {"Memory (KB)",-15} {"Estimate",-15} {"Error %",-10}");
        Console.WriteLine(new string('-', 70));

        foreach (var precision in precisions)
        {
            var hll = new HyperLogLog(precision);
            
            // Add elements
            for (int i = 0; i < actualCount; i++)
            {
                hll.Add($"user_{i}");
            }

            var estimate = hll.EstimateCardinality();
            var error = Math.Abs(actualCount - estimate) / (double)actualCount * 100;
            var buckets = 1 << precision;
            var memoryKB = buckets / 1024.0;

            Console.WriteLine($"{precision,-10} {buckets,-10} {memoryKB,-15:F2} {estimate,-15:N0} {error,-10:F2}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Benchmarks HyperLogLog performance with large datasets
    /// </summary>
    public static async Task PerformanceBenchmark()
    {
        Console.WriteLine("=== HyperLogLog Performance Benchmark ===\n");

        var dataSizes = new[] { 10000, 100000, 1000000, 10000000 };
        var precision = 14;

        Console.WriteLine($"Using precision: {precision}\n");
        Console.WriteLine($"{"Dataset Size",-15} {"Add Time (ms)",-20} {"Estimate Time (ms)",-20} {"Estimate",-15} {"Error %",-10}");
        Console.WriteLine(new string('-', 90));

        foreach (var size in dataSizes)
        {
            var hll = new HyperLogLog(precision);
            
            // Benchmark Add operations
            var addSw = System.Diagnostics.Stopwatch.StartNew();
            await Task.Run(() =>
            {
                for (int i = 0; i < size; i++)
                {
                    hll.Add($"user_{i}");
                }
            });
            addSw.Stop();

            // Benchmark Estimate operation
            var estimateSw = System.Diagnostics.Stopwatch.StartNew();
            var estimate = hll.EstimateCardinality();
            estimateSw.Stop();

            var error = Math.Abs(size - estimate) / (double)size * 100;

            Console.WriteLine($"{size,-15:N0} {addSw.ElapsedMilliseconds,-20} {estimateSw.ElapsedMilliseconds,-20} {estimate,-15:N0} {error,-10:F2}");
        }
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates memory efficiency compared to exact counting
    /// </summary>
    public static void MemoryComparison()
    {
        Console.WriteLine("=== HyperLogLog Memory Efficiency ===\n");

        const int itemCount = 1000000;
        var precision = 14;
        var hllMemoryBytes = 1 << precision; // One byte per bucket
        
        // Estimate memory for HashSet (rough approximation)
        // Each string pointer is 8 bytes + string overhead (~40 bytes per string on average)
        var hashSetMemoryBytes = itemCount * (8 + 40);

        Console.WriteLine($"Storing 1,000,000 unique elements:\n");
        Console.WriteLine($"HashSet (exact):     {hashSetMemoryBytes / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"HyperLogLog (est):   {hllMemoryBytes / 1024.0:F2} KB");
        Console.WriteLine($"Memory savings:      {(1 - (hllMemoryBytes / (double)hashSetMemoryBytes)) * 100:F2}%\n");
    }

    /// <summary>
    /// Runs all HyperLogLog examples
    /// </summary>
    public static async Task RunAllExamplesAsync()
    {
        BasicExample();
        DuplicateExample();
        MergeExample();
        PrecisionBenchmark();
        await PerformanceBenchmark();
        MemoryComparison();
    }
}
