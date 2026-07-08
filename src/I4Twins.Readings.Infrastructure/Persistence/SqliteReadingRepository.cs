using I4Twins.Readings.Application.Abstractions;
using I4Twins.Readings.Domain.Readings;
using Microsoft.EntityFrameworkCore;

namespace I4Twins.Readings.Infrastructure.Persistence;

public sealed class SqliteReadingRepository : IReadingRepository
{
    private readonly ReadingsDbContext _dbContext;

    public SqliteReadingRepository(ReadingsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StoreReadingResult> StoreIfNotExistsAsync(
        Reading reading,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var timestampUtcTicks = reading.TimestampUtc.UtcTicks;

        var existingReading = await FindByIdentityAsync(
            reading.DeviceId,
            reading.Metric,
            timestampUtcTicks,
            reading.Seq,
            cancellationToken);

        if (existingReading is not null)
        {
            return existingReading.Value.Equals(reading.Value)
                ? StoreReadingResult.Duplicate()
                : StoreReadingResult.ConflictingDuplicate();
        }

        _dbContext.Readings.Add(ReadingEntity.FromDomain(reading));

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return StoreReadingResult.Stored();
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();

            var readingAfterConflict = await FindByIdentityAsync(
                reading.DeviceId,
                reading.Metric,
                timestampUtcTicks,
                reading.Seq,
                cancellationToken);

            if (readingAfterConflict is null)
            {
                throw;
            }

            return readingAfterConflict.Value.Equals(reading.Value)
                ? StoreReadingResult.Duplicate()
                : StoreReadingResult.ConflictingDuplicate();
        }
    }

    public async Task<IReadOnlyList<Reading>> GetByDeviceMetricAndTimeRangeAsync(
        string deviceId,
        string metric,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default)
    {
        var normalizedDeviceId = deviceId.Trim();
        var normalizedMetric = metric.Trim();

        var fromTicks = fromUtc.UtcTicks;
        var toTicks = toUtc.UtcTicks;

        var entities = await _dbContext.Readings
            .AsNoTracking()
            .Where(reading =>
                reading.DeviceId == normalizedDeviceId &&
                reading.Metric == normalizedMetric &&
                reading.TimestampUtcTicks >= fromTicks &&
                reading.TimestampUtcTicks < toTicks)
            .OrderBy(reading => reading.TimestampUtcTicks)
            .ToListAsync(cancellationToken);

        return entities
            .Select(entity => entity.ToDomain())
            .ToList();
    }

    private async Task<ReadingEntity?> FindByIdentityAsync(
        string deviceId,
        string metric,
        long timestampUtcTicks,
        int seq,
        CancellationToken cancellationToken)
    {
        return await _dbContext.Readings
            .AsNoTracking()
            .FirstOrDefaultAsync(reading =>
                reading.DeviceId == deviceId &&
                reading.Metric == metric &&
                reading.TimestampUtcTicks == timestampUtcTicks &&
                reading.Seq == seq,
                cancellationToken);
    }
}