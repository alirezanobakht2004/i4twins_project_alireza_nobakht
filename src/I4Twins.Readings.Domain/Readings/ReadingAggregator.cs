namespace I4Twins.Readings.Domain.Readings;

public sealed class ReadingAggregator
{
    public IReadOnlyList<ReadingAggregationBucket> Aggregate(
        IEnumerable<Reading> readings,
        string deviceId,
        string metric,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        TimeSpan bucketSize)
    {
        ArgumentNullException.ThrowIfNull(readings);

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            throw new ArgumentException("DeviceId is required.", nameof(deviceId));
        }

        if (string.IsNullOrWhiteSpace(metric))
        {
            throw new ArgumentException("Metric is required.", nameof(metric));
        }

        if (fromUtc >= toUtc)
        {
            throw new ArgumentException("From must be earlier than To.", nameof(fromUtc));
        }

        if (bucketSize <= TimeSpan.Zero)
        {
            throw new ArgumentException("Bucket size must be greater than zero.", nameof(bucketSize));
        }

        var normalizedDeviceId = deviceId.Trim();
        var normalizedMetric = metric.Trim();

        return readings
            .Where(reading =>
                reading.DeviceId == normalizedDeviceId &&
                reading.Metric == normalizedMetric &&
                reading.TimestampUtc >= fromUtc &&
                reading.TimestampUtc < toUtc)
            .GroupBy(reading => CalculateBucketStart(
                reading.TimestampUtc,
                fromUtc,
                bucketSize))
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var values = group.Select(reading => reading.Value).ToList();

                return new ReadingAggregationBucket(
                    BucketStartUtc: group.Key,
                    Count: values.Count,
                    Average: values.Average(),
                    Min: values.Min(),
                    Max: values.Max());
            })
            .ToList();
    }

    private static DateTimeOffset CalculateBucketStart(
        DateTimeOffset timestampUtc,
        DateTimeOffset fromUtc,
        TimeSpan bucketSize)
    {
        var elapsedTicks = (timestampUtc - fromUtc).Ticks;
        var bucketIndex = elapsedTicks / bucketSize.Ticks;

        return fromUtc.AddTicks(bucketIndex * bucketSize.Ticks);
    }
}