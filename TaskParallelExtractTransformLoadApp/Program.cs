using TaskParallelExtractTransform_LoadApp.Extensions;

var builder = WebApplication.CreateBuilder(args);
        
builder.Services.AddTPLETLPipeline();

var app = builder.Build();

app.UseTPLETLPipeline();

await app.RunAsync();