namespace I4Twins.Readings.Application.Ingestion;

public sealed record RawReadingRecord(
    int LineNumber,
    string? DeviceId,
    string? Metric,
    string? Timestamp,
    double? Value,
    int? Seq,
    string? ParseError = null)
{
    public bool HasParseError => !string.IsNullOrWhiteSpace(ParseError);
}