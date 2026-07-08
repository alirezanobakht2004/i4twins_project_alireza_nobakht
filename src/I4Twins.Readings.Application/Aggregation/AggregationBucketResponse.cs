namespace I4Twins.Readings.Application.Aggregation;

public sealed record AggregationBucketResponse(
    DateTimeOffset BucketStartUtc,
    int Count,
    double Average,
    double Min,
    double Max);