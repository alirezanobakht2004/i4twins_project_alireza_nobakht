using I4Twins.Readings.Domain.Readings;

namespace I4Twins.Readings.Tests.Readings;

public sealed class ReadingAggregatorTests
{
    [Fact]
    public void Aggregate_ReturnsCorrectStatistics_ForSingleBucket()
    {
        var readings = new[]
        {
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:00:10Z", 10, 1),
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:00:20Z", 20, 2),
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:00:30Z", 30, 3)
        };

        var aggregator = new ReadingAggregator();

        var result = aggregator.Aggregate(
            readings,
            deviceId: "PUMP-01",
            metric: "temperature",
            fromUtc: Utc("2025-06-01T08:00:00Z"),
            toUtc: Utc("2025-06-01T08:01:00Z"),
            bucketSize: TimeSpan.FromMinutes(1));

        Assert.Single(result);

        var bucket = result[0];

        Assert.Equal(Utc("2025-06-01T08:00:00Z"), bucket.BucketStartUtc);
        Assert.Equal(3, bucket.Count);
        Assert.Equal(20, bucket.Average);
        Assert.Equal(10, bucket.Min);
        Assert.Equal(30, bucket.Max);
    }

    [Fact]
    public void Aggregate_GroupsOutOfOrderReadings_IntoCorrectBuckets()
    {
        var readings = new[]
        {
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:02:10Z", 30, 3),
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:00:10Z", 10, 1),
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:01:10Z", 20, 2)
        };

        var aggregator = new ReadingAggregator();

        var result = aggregator.Aggregate(
            readings,
            deviceId: "PUMP-01",
            metric: "temperature",
            fromUtc: Utc("2025-06-01T08:00:00Z"),
            toUtc: Utc("2025-06-01T08:03:00Z"),
            bucketSize: TimeSpan.FromMinutes(1));

        Assert.Equal(3, result.Count);

        Assert.Equal(Utc("2025-06-01T08:00:00Z"), result[0].BucketStartUtc);
        Assert.Equal(10, result[0].Average);

        Assert.Equal(Utc("2025-06-01T08:01:00Z"), result[1].BucketStartUtc);
        Assert.Equal(20, result[1].Average);

        Assert.Equal(Utc("2025-06-01T08:02:00Z"), result[2].BucketStartUtc);
        Assert.Equal(30, result[2].Average);
    }

    [Fact]
    public void Aggregate_UsesInclusiveFromAndExclusiveTo()
    {
        var readings = new[]
        {
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:00:00Z", 10, 1),
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:00:59Z", 20, 2),
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:01:00Z", 99, 3)
        };

        var aggregator = new ReadingAggregator();

        var result = aggregator.Aggregate(
            readings,
            deviceId: "PUMP-01",
            metric: "temperature",
            fromUtc: Utc("2025-06-01T08:00:00Z"),
            toUtc: Utc("2025-06-01T08:01:00Z"),
            bucketSize: TimeSpan.FromMinutes(1));

        Assert.Single(result);

        Assert.Equal(2, result[0].Count);
        Assert.Equal(15, result[0].Average);
        Assert.Equal(10, result[0].Min);
        Assert.Equal(20, result[0].Max);
    }

    [Fact]
    public void Aggregate_OmitsEmptyBuckets()
    {
        var readings = new[]
        {
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:00:10Z", 10, 1),
            CreateReading("PUMP-01", "temperature", "2025-06-01T08:02:10Z", 30, 2)
        };

        var aggregator = new ReadingAggregator();

        var result = aggregator.Aggregate(
            readings,
            deviceId: "PUMP-01",
            metric: "temperature",
            fromUtc: Utc("2025-06-01T08:00:00Z"),
            toUtc: Utc("2025-06-01T08:03:00Z"),
            bucketSize: TimeSpan.FromMinutes(1));

        Assert.Equal(2, result.Count);
        Assert.Equal(Utc("2025-06-01T08:00:00Z"), result[0].BucketStartUtc);
        Assert.Equal(Utc("2025-06-01T08:02:00Z"), result[1].BucketStartUtc);
    }

    private static Reading CreateReading(
        string deviceId,
        string metric,
        string timestamp,
        double value,
        int seq)
    {
        var result = Reading.Create(
            deviceId,
            metric,
            timestamp,
            value,
            seq);

        Assert.True(result.IsValid, string.Join("; ", result.Errors));

        return result.Reading!;
    }

    private static DateTimeOffset Utc(string value)
    {
        return DateTimeOffset.Parse(value).ToUniversalTime();
    }
}