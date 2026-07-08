using I4Twins.Readings.Application.Abstractions;
using I4Twins.Readings.Infrastructure.Persistence;
using I4Twins.Readings.Infrastructure.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace I4Twins.Readings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddReadingsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = configuration.GetConnectionString("ReadingsDatabase")
            ?? "Data Source=readings.db";

        services.AddDbContext<ReadingsDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        services.AddScoped<IReadingRepository, SqliteReadingRepository>();
        services.AddScoped<DatabaseInitializer>();

        var readingsFilePath = configuration["Readings:FilePath"]
            ?? Path.Combine("..", "..", "assets", "readings.jsonl");

        if (!Path.IsPathRooted(readingsFilePath))
        {
            readingsFilePath = Path.GetFullPath(
                Path.Combine(contentRootPath, readingsFilePath));
        }

        services.AddSingleton<IReadingSource>(_ =>
            new JsonlReadingSource(readingsFilePath));

        return services;
    }
}