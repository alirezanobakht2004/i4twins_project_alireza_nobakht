using I4Twins.Readings.Domain.Readings;

namespace I4Twins.Readings.Application.Abstractions;

public interface IReadingRepository
{
    Task<StoreReadingResult> StoreIfNotExistsAsync(
        Reading reading,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reading>> GetByDeviceMetricAndTimeRangeAsync(
        string deviceId,
        string metric,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken = default);
}