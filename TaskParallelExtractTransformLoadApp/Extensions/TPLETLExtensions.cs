// Extension methods for service configuration
using System.Runtime.CompilerServices;
using TaskParallelExtractTransform_LoadApp.Interfaces;
using TaskParallelExtractTransform_LoadApp.Middlewares;
using TaskParallelExtractTransform_LoadApp.Samples;
using TaskParallelExtractTransformLoadApp.Samples;

namespace TaskParallelExtractTransform_LoadApp.Extensions
{
    public static class TPLETLExtensions
    {
        public static IServiceCollection AddTPLETLPipeline(this IServiceCollection services)
        {
            services.AddScoped<IETLPipeline, TPLDataflowPipeline>();
            services.AddScoped<IDataSource, SampleDataSource>();
            services.AddScoped<IDataTransformer, SampleTransformer>();
            services.AddScoped<IDataLoader, SampleLoader>();
            return services;
        }

        public static IApplicationBuilder UseTPLETLPipeline(this IApplicationBuilder app)
        {
            return app.UseMiddleware<TPLETLMiddleware>();
        }
    }
}


   