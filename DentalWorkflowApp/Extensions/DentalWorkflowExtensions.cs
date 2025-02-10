using DentalWorkflowApp.Pipelines;
using DentalWorkflowApp.Services;

namespace DentalWorkflowApp.Extensions
{
    public static class DentalWorkflowExtensions
    {
        public static IServiceCollection AddDentalWorkflow(this IServiceCollection services)
        {
            services.AddSingleton<DentalWorkflowPipeline>();
            services.AddSingleton<DentalResourceManager>();
            return services;
        }

        public static IApplicationBuilder UseDentalWorkflow(this IApplicationBuilder app)
        {
            return app.UseMiddleware<DentalWorkflowMiddleware>();
        }
    }
}