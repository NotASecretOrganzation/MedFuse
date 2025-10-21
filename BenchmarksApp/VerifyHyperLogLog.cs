using System;
using System.Linq;

/// <summary>
/// Verification tests for HyperLogLog implementation
/// </summary>
public class HyperLogLogVerification
{
    public static bool RunAllTests()
    {
        Console.WriteLine("=== HyperLogLog Verification Tests ===\n");
        
        bool allPassed = true;
        allPassed &= TestBasicCardinality();
        allPassed &= TestDuplicates();
        allPassed &= TestMerge();
        allPassed &= TestPrecisionRange();
        allPassed &= TestEmptyHLL();
        allPassed &= TestLargeDataset();
        
        Console.WriteLine(allPassed ? "\n✓ All tests passed!" : "\n✗ Some tests failed!");
        return allPassed;
    }

    private static bool TestBasicCardinality()
    {
        Console.WriteLine("Test: Basic Cardinality Estimation");
        var hll = new HyperLogLog(precision: 14);
        
        for (int i = 0; i < 10000; i++)
        {
            hll.Add($"element_{i}");
        }
        
        var estimate = hll.EstimateCardinality();
        var error = Math.Abs(10000 - estimate) / 10000.0 * 100;
        
        bool passed = error < 5.0; // Accept up to 5% error
        Console.WriteLine($"  Expected: 10,000 | Estimated: {estimate} | Error: {error:F2}% | {(passed ? "PASS" : "FAIL")}\n");
        return passed;
    }

    private static bool TestDuplicates()
    {
        Console.WriteLine("Test: Duplicate Handling");
        var hll = new HyperLogLog(precision: 14);
        
        // Add 1000 unique elements, but repeat each 10 times
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 1000; j++)
            {
                hll.Add($"element_{j}");
            }
        }
        
        var estimate = hll.EstimateCardinality();
        var error = Math.Abs(1000 - estimate) / 1000.0 * 100;
        
        bool passed = error < 10.0; // Accept up to 10% error for smaller datasets
        Console.WriteLine($"  Expected: 1,000 | Estimated: {estimate} | Error: {error:F2}% | {(passed ? "PASS" : "FAIL")}\n");
        return passed;
    }

    private static bool TestMerge()
    {
        Console.WriteLine("Test: Merge Functionality");
        var hll1 = new HyperLogLog(precision: 12);
        var hll2 = new HyperLogLog(precision: 12);
        
        // Add 0-4999 to hll1
        for (int i = 0; i < 5000; i++)
        {
            hll1.Add($"element_{i}");
        }
        
        // Add 5000-9999 to hll2 (no overlap)
        for (int i = 5000; i < 10000; i++)
        {
            hll2.Add($"element_{i}");
        }
        
        hll1.Merge(hll2);
        var estimate = hll1.EstimateCardinality();
        var error = Math.Abs(10000 - estimate) / 10000.0 * 100;
        
        bool passed = error < 5.0;
        Console.WriteLine($"  Expected: 10,000 | Estimated: {estimate} | Error: {error:F2}% | {(passed ? "PASS" : "FAIL")}\n");
        return passed;
    }

    private static bool TestPrecisionRange()
    {
        Console.WriteLine("Test: Precision Range Validation");
        bool passed = true;
        
        try
        {
            var _ = new HyperLogLog(precision: 3); // Too low
            Console.WriteLine("  Should reject precision < 4 | FAIL\n");
            passed = false;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("  Correctly rejects precision < 4 | PASS");
        }
        
        try
        {
            var _ = new HyperLogLog(precision: 17); // Too high
            Console.WriteLine("  Should reject precision > 16 | FAIL\n");
            passed = false;
        }
        catch (ArgumentException)
        {
            Console.WriteLine("  Correctly rejects precision > 16 | PASS");
        }
        
        try
        {
            var _ = new HyperLogLog(precision: 10); // Valid
            Console.WriteLine("  Accepts valid precision (10) | PASS\n");
        }
        catch
        {
            Console.WriteLine("  Should accept valid precision | FAIL\n");
            passed = false;
        }
        
        return passed;
    }

    private static bool TestEmptyHLL()
    {
        Console.WriteLine("Test: Empty HyperLogLog");
        var hll = new HyperLogLog(precision: 12);
        
        var estimate = hll.EstimateCardinality();
        bool passed = estimate == 0;
        
        Console.WriteLine($"  Expected: 0 | Estimated: {estimate} | {(passed ? "PASS" : "FAIL")}\n");
        return passed;
    }

    private static bool TestLargeDataset()
    {
        Console.WriteLine("Test: Large Dataset (100K elements)");
        var hll = new HyperLogLog(precision: 14);
        
        for (int i = 0; i < 100000; i++)
        {
            hll.Add($"element_{i}");
        }
        
        var estimate = hll.EstimateCardinality();
        var error = Math.Abs(100000 - estimate) / 100000.0 * 100;
        
        bool passed = error < 2.0; // Stricter for larger datasets
        Console.WriteLine($"  Expected: 100,000 | Estimated: {estimate} | Error: {error:F2}% | {(passed ? "PASS" : "FAIL")}\n");
        return passed;
    }
}
