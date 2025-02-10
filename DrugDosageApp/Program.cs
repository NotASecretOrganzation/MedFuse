using DrugDosageApp.Contexts;
using DrugDosageApp.Extensions;
using DrugDosageApp.Services;

var builder = WebApplication.CreateBuilder(args);
        
builder.Services.AddDrugDosageValidation();

var app = builder.Build();

app.UseDrugDosageValidation();

// Train the model
var classifier = app.Services.GetRequiredService<DrugDosageClassifier>();
await classifier.TrainModelAsync(SampleDataGenerator.GenerateTrainingData());

await app.RunAsync();