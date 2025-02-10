var builder = WebApplication.CreateBuilder(args);
        
// Add services
builder.Services.AddETLPipeline();

var app = builder.Build();

// Configure middleware pipeline
app.UseETLPipeline();

app.MapGet("/start-etl", async (HttpContext context) =>
{
    var etlContext = context.RequestServices.GetRequiredService<ETLContext>();
    return Results.Ok(new { JobId = etlContext.JobId });
});

await app.RunAsync();