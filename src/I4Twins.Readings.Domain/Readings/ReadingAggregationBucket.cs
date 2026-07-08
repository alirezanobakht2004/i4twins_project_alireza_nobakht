namespace I4Twins.Readings.Domain.Readings;

public sealed record ReadingAggregationBucket(
    DateTimeOffset BucketStartUtc,
    int Count,
    double Average,
    double Min,
    double Max);