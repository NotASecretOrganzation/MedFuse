using DentalWorkflowApp.Extensions;

var builder = WebApplication.CreateBuilder(args);
        
builder.Services.AddDentalWorkflow();

var app = builder.Build();

app.UseDentalWorkflow();

await app.RunAsync();