var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();

// Run sample benchmark
await DivideAndConquerExamples.RunBenchmarksAsync();

await app.RunAsync();