namespace I4Twins.Readings.Domain.Readings;

public sealed record ReadingIdentity(
    string DeviceId,
    string Metric,
    DateTimeOffset TimestampUtc,
    int Seq);