// Extension Methods for DI and Middleware Registration
using ExtractTransformLoad.Interfaces;
using ExtractTransformLoad.Middlewares;
using ExtractTransformLoad.Models;

public static class ETLMiddlewareExtensions
{
    public static IServiceCollection AddETLPipeline(this IServiceCollection services)
    {
        services.AddScoped<ETLContext>();
        services.AddScoped<IDataSource, SampleDataSource>();
        services.AddScoped<IDataStore, SampleDataStore>();
        return services;
    }

    public static IApplicationBuilder UseETLPipeline(this IApplicationBuilder app)
    {
        app.UseMiddleware<ETLPipelineMiddleware>();
        app.UseMiddleware<ExtractionMiddleware>();
        app.UseMiddleware<TransformationMiddleware>();
        app.UseMiddleware<LoadingMiddleware>();
        app.UseMiddleware<ETLProgressMiddleware>();
        return app;
    }
}