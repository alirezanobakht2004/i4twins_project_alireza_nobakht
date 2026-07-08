namespace I4Twins.Readings.Application.Aggregation;

public sealed record AggregationQuery(
    string DeviceId,
    string Metric,
    DateTimeOffset FromUtc,
    DateTimeOffset ToUtc,
    int BucketSeconds);