using I4Twins.Readings.Domain.Readings;

namespace I4Twins.Readings.Tests.Readings;

public sealed class ReadingDeduplicatorTests
{
    [Fact]
    public void Deduplicate_KeepsSingleReading_WhenIdentityIsRepeated()
    {
        var first = CreateReading(
            deviceId: "PUMP-01",
            metric: "temperature",
            timestamp: "2025-06-01T08:00:00Z",
            value: 70.1,
            seq: 100);

        var duplicate = CreateReading(
            deviceId: "PUMP-01",
            metric: "temperature",
            timestamp: "2025-06-01T08:00:00Z",
            value: 70.1,
            seq: 100);

        var deduplicator = new ReadingDeduplicator();

        var result = deduplicator.Deduplicate(new[]
        {
            first,
            duplicate
        });

        Assert.Single(result.UniqueReadings);
        Assert.Equal(1, result.DuplicatesRemoved);
        Assert.Equal(0, result.ConflictingDuplicates);
        Assert.Equal(70.1, result.UniqueReadings[0].Value);
    }

    [Fact]
    public void Deduplicate_KeepsFirstReading_WhenDuplicateHasDifferentValue()
    {
        var first = CreateReading(
            deviceId: "PUMP-01",
            metric: "temperature",
            timestamp: "2025-06-01T08:00:00Z",
            value: 70.1,
            seq: 100);

        var conflictingDuplicate = CreateReading(
            deviceId: "PUMP-01",
            metric: "temperature",
            timestamp: "2025-06-01T08:00:00Z",
            value: 99.9,
            seq: 100);

        var deduplicator = new ReadingDeduplicator();

        var result = deduplicator.Deduplicate(new[]
        {
            first,
            conflictingDuplicate
        });

        Assert.Single(result.UniqueReadings);
        Assert.Equal(1, result.DuplicatesRemoved);
        Assert.Equal(1, result.ConflictingDuplicates);
        Assert.Equal(70.1, result.UniqueReadings[0].Value);
    }

    [Fact]
    public void Deduplicate_DoesNotRemoveReading_WhenSeqIsDifferent()
    {
        var first = CreateReading(
            deviceId: "PUMP-01",
            metric: "temperature",
            timestamp: "2025-06-01T08:00:00Z",
            value: 70.1,
            seq: 100);

        var second = CreateReading(
            deviceId: "PUMP-01",
            metric: "temperature",
            timestamp: "2025-06-01T08:00:00Z",
            value: 70.1,
            seq: 101);

        var deduplicator = new ReadingDeduplicator();

        var result = deduplicator.Deduplicate(new[]
        {
            first,
            second
        });

        Assert.Equal(2, result.UniqueReadings.Count);
        Assert.Equal(0, result.DuplicatesRemoved);
        Assert.Equal(0, result.ConflictingDuplicates);
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
}