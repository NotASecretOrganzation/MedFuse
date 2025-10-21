using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Controller for HyperLogLog cardinality estimation examples and benchmarks
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HyperLogLogController : ControllerBase
{
    /// <summary>
    /// Runs all HyperLogLog examples
    /// </summary>
    [HttpPost("examples")]
    public async Task<IActionResult> RunExamples()
    {
        await HyperLogLogExamples.RunAllExamplesAsync();
        return Ok(new { message = "HyperLogLog examples completed. Check console output for results." });
    }

    /// <summary>
    /// Demonstrates basic HyperLogLog usage
    /// </summary>
    [HttpPost("basic")]
    public IActionResult BasicExample()
    {
        HyperLogLogExamples.BasicExample();
        return Ok(new { message = "Basic example completed. Check console output for results." });
    }

    /// <summary>
    /// Demonstrates HyperLogLog with duplicate elements
    /// </summary>
    [HttpPost("duplicates")]
    public IActionResult DuplicateExample()
    {
        HyperLogLogExamples.DuplicateExample();
        return Ok(new { message = "Duplicate example completed. Check console output for results." });
    }

    /// <summary>
    /// Demonstrates merging HyperLogLog instances
    /// </summary>
    [HttpPost("merge")]
    public IActionResult MergeExample()
    {
        HyperLogLogExamples.MergeExample();
        return Ok(new { message = "Merge example completed. Check console output for results." });
    }

    /// <summary>
    /// Benchmarks HyperLogLog accuracy across different precisions
    /// </summary>
    [HttpPost("precision-benchmark")]
    public IActionResult PrecisionBenchmark()
    {
        HyperLogLogExamples.PrecisionBenchmark();
        return Ok(new { message = "Precision benchmark completed. Check console output for results." });
    }

    /// <summary>
    /// Benchmarks HyperLogLog performance with large datasets
    /// </summary>
    [HttpPost("performance-benchmark")]
    public async Task<IActionResult> PerformanceBenchmark()
    {
        await HyperLogLogExamples.PerformanceBenchmark();
        return Ok(new { message = "Performance benchmark completed. Check console output for results." });
    }

    /// <summary>
    /// Estimates cardinality for a custom dataset
    /// </summary>
    [HttpPost("estimate")]
    public IActionResult EstimateCardinality([FromBody] EstimateRequest request)
    {
        if (request.Elements == null || !request.Elements.Any())
        {
            return BadRequest(new { error = "Elements array cannot be empty" });
        }

        var precision = request.Precision ?? 14;
        if (precision < 4 || precision > 16)
        {
            return BadRequest(new { error = "Precision must be between 4 and 16" });
        }

        var hll = new HyperLogLog(precision);
        foreach (var element in request.Elements)
        {
            hll.Add(element);
        }

        var estimate = hll.EstimateCardinality();
        var actualUnique = request.Elements.Distinct().Count();
        var error = Math.Abs(actualUnique - estimate) / (double)actualUnique * 100;

        return Ok(new
        {
            precision,
            actualUniqueCount = actualUnique,
            estimatedCount = estimate,
            errorPercentage = error,
            memoryUsedBytes = 1 << precision
        });
    }

    /// <summary>
    /// Runs verification tests for HyperLogLog implementation
    /// </summary>
    [HttpPost("verify")]
    public IActionResult RunVerificationTests()
    {
        var success = HyperLogLogVerification.RunAllTests();
        return Ok(new
        {
            success,
            message = success
                ? "All verification tests passed. Check console output for details."
                : "Some verification tests failed. Check console output for details."
        });
    }
}

/// <summary>
/// Request model for cardinality estimation
/// </summary>
public class EstimateRequest
{
    public string[] Elements { get; set; } = Array.Empty<string>();
    public int? Precision { get; set; }
}
