// Extension methods for service configuration
using DrugDosageApp.Contexts;
using DrugDosageApp.Middlewares;
using DrugDosageApp.Services;

namespace DrugDosageApp.Extensions
{
    public static class DrugDosageExtensions
    {
        public static IServiceCollection AddDrugDosageValidation(this IServiceCollection services)
        {
            services.AddSingleton<NDC11Validator>();
            services.AddSingleton<DrugDosageClassifier>();
            return services;
        }

        public static IApplicationBuilder UseDrugDosageValidation(this IApplicationBuilder app)
        {
            return app.UseMiddleware<DrugDosageMiddleware>();
        }
    }
}