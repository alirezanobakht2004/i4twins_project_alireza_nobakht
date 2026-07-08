using I4Twins.Readings.Application.Abstractions;
using I4Twins.Readings.Domain.Readings;

namespace I4Twins.Readings.Application.Aggregation;

public sealed class GetAggregatedReadingsService
{
    private readonly IReadingRepository _readingRepository;
    private readonly ReadingAggregator _readingAggregator;

    public GetAggregatedReadingsService(
        IReadingRepository readingRepository,
        ReadingAggregator readingAggregator)
    {
        _readingRepository = readingRepository;
        _readingAggregator = readingAggregator;
    }

    public async Task<IReadOnlyList<AggregationBucketResponse>> GetAsync(
        AggregationQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var bucketSize = TimeSpan.FromSeconds(query.BucketSeconds);

        var readings = await _readingRepository.GetByDeviceMetricAndTimeRangeAsync(
            query.DeviceId,
            query.Metric,
            query.FromUtc,
            query.ToUtc,
            cancellationToken);

        var buckets = _readingAggregator.Aggregate(
            readings,
            query.DeviceId,
            query.Metric,
            query.FromUtc,
            query.ToUtc,
            bucketSize);

        return buckets
            .Select(bucket => new AggregationBucketResponse(
                BucketStartUtc: bucket.BucketStartUtc,
                Count: bucket.Count,
                Average: bucket.Average,
                Min: bucket.Min,
                Max: bucket.Max))
            .ToList();
    }
}