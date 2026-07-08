using System.Globalization;

namespace I4Twins.Readings.Domain.Readings;

public sealed class Reading
{
    private Reading(
        string deviceId,
        string metric,
        DateTimeOffset timestampUtc,
        double value,
        int seq)
    {
        DeviceId = deviceId;
        Metric = metric;
        TimestampUtc = timestampUtc;
        Value = value;
        Seq = seq;
    }

    public string DeviceId { get; }

    public string Metric { get; }

    public DateTimeOffset TimestampUtc { get; }

    public double Value { get; }

    public int Seq { get; }

    public ReadingIdentity Identity => new(DeviceId, Metric, TimestampUtc, Seq);

    public static ReadingValidationResult Create(
        string? deviceId,
        string? metric,
        string? timestamp,
        double? value,
        int? seq)
    {
        var errors = new List<string>();

        var normalizedDeviceId = NormalizeRequiredText(deviceId, nameof(DeviceId), errors);
        var normalizedMetric = NormalizeRequiredText(metric, nameof(Metric), errors);

        var parsedTimestamp = ParseUtcTimestamp(timestamp, errors);

        if (value is null)
        {
            errors.Add("Value is required and must be numeric.");
        }
        else if (!double.IsFinite(value.Value))
        {
            errors.Add("Value must be a finite number.");
        }

        if (seq is null)
        {
            errors.Add("Seq is required and must be an integer.");
        }

        if (errors.Count > 0)
        {
            return ReadingValidationResult.Failure(errors);
        }

        var reading = new Reading(
            normalizedDeviceId!,
            normalizedMetric!,
            parsedTimestamp!.Value,
            value!.Value,
            seq!.Value);

        return ReadingValidationResult.Success(reading);
    }

    private static string? NormalizeRequiredText(
        string? value,
        string fieldName,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required.");
            return null;
        }

        return value.Trim();
    }

    private static DateTimeOffset? ParseUtcTimestamp(
        string? timestamp,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
        {
            errors.Add("Timestamp is required.");
            return null;
        }

        if (!DateTimeOffset.TryParse(
                timestamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedTimestamp))
        {
            errors.Add("Timestamp must be a valid ISO-8601 UTC value.");
            return null;
        }

        if (parsedTimestamp.Offset != TimeSpan.Zero)
        {
            errors.Add("Timestamp must be in UTC.");
            return null;
        }

        return parsedTimestamp;
    }
}