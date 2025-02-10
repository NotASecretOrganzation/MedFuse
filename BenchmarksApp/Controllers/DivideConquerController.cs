// Example usage with ASP.NET Core endpoint
using Microsoft.AspNetCore.Mvc;

public class DivideConquerController : Microsoft.AspNetCore.Mvc.ControllerBase
{
    [Microsoft.AspNetCore.Mvc.HttpPost("benchmark")]
    public async Task<IActionResult> RunBenchmark()
    {
        await DivideAndConquerExamples.RunBenchmarksAsync();
        return Ok("Benchmarks completed");
    }
}
