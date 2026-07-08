using I4Twins.Readings.Application;
using I4Twins.Readings.Application.Aggregation;
using I4Twins.Readings.Application.Ingestion;
using I4Twins.Readings.Infrastructure;
using I4Twins.Readings.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReadingsApplication();

builder.Services.AddReadingsInfrastructure(
    builder.Configuration,
    builder.Environment.ContentRootPath);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var databaseInitializer = scope.ServiceProvider
        .GetRequiredService<DatabaseInitializer>();

    await databaseInitializer.InitializeAsync();
}

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "i4twins-readings-service"
}));

app.MapPost("/api/ingestion", async (
    IngestReadingsService ingestionService,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    logger.LogInformation("Reading ingestion started.");

    var report = await ingestionService.IngestAsync(cancellationToken);

    logger.LogInformation(
        "Reading ingestion completed. TotalLines={TotalLines}, StoredReadings={StoredReadings}, DuplicatesRemoved={DuplicatesRemoved}, InvalidRecordsRejected={InvalidRecordsRejected}, ConflictingDuplicates={ConflictingDuplicates}",
        report.TotalLines,
        report.StoredReadings,
        report.DuplicatesRemoved,
        report.InvalidRecordsRejected,
        report.ConflictingDuplicates);

    return Results.Ok(report);
});

app.MapGet("/api/aggregations", async (
    string deviceId,
    string metric,
    DateTimeOffset from,
    DateTimeOffset to,
    int bucketSeconds,
    GetAggregatedReadingsService aggregationService,
    CancellationToken cancellationToken) =>
{
    var errors = ValidateAggregationRequest(
        deviceId,
        metric,
        from,
        to,
        bucketSeconds);

    if (errors.Count > 0)
    {
        return Results.BadRequest(new
        {
            errors
        });
    }

    var query = new AggregationQuery(
        DeviceId: deviceId,
        Metric: metric,
        FromUtc: from,
        ToUtc: to,
        BucketSeconds: bucketSeconds);

    var result = await aggregationService.GetAsync(
        query,
        cancellationToken);

    return Results.Ok(result);
});

app.Run();

static List<string> ValidateAggregationRequest(
    string? deviceId,
    string? metric,
    DateTimeOffset from,
    DateTimeOffset to,
    int bucketSeconds)
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(deviceId))
    {
        errors.Add("deviceId is required.");
    }

    if (string.IsNullOrWhiteSpace(metric))
    {
        errors.Add("metric is required.");
    }

    if (from.Offset != TimeSpan.Zero)
    {
        errors.Add("from must be a UTC timestamp.");
    }

    if (to.Offset != TimeSpan.Zero)
    {
        errors.Add("to must be a UTC timestamp.");
    }

    if (from >= to)
    {
        errors.Add("from must be earlier than to.");
    }

    if (bucketSeconds <= 0)
    {
        errors.Add("bucketSeconds must be greater than zero.");
    }

    return errors;
}