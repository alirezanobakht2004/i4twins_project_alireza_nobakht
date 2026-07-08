using I4Twins.Readings.Application.Aggregation;
using I4Twins.Readings.Application.Ingestion;
using I4Twins.Readings.Domain.Readings;
using Microsoft.Extensions.DependencyInjection;

namespace I4Twins.Readings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddReadingsApplication(this IServiceCollection services)
    {
        services.AddSingleton<ReadingDeduplicator>();
        services.AddSingleton<ReadingAggregator>();

        services.AddScoped<IngestReadingsService>();
        services.AddScoped<GetAggregatedReadingsService>();

        return services;
    }
}