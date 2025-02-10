using DrugDosageApp.Contexts;
using DrugDosageApp.Models;

namespace DrugDosageApp.Middlewares
{
    public class DrugDosageMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DrugDosageClassifier _classifier;
        private readonly ILogger<DrugDosageMiddleware> _logger;

        public DrugDosageMiddleware(
            RequestDelegate next,
            DrugDosageClassifier classifier,
            ILogger<DrugDosageMiddleware> logger)
        {
            _next = next;
            _classifier = classifier;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/api/validate-dosage")
            {
                try
                {
                    var dosage = await context.Request.ReadFromJsonAsync<DrugDosage>();
                    var result = await _classifier.ValidateDosageAsync(dosage);
                    await context.Response.WriteAsJsonAsync(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating drug dosage");
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(new { error = "Internal server error" });
                }
                return;
            }

            await _next(context);
        }
    }
}