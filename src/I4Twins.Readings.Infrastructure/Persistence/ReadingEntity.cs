using System.Globalization;
using I4Twins.Readings.Domain.Readings;

namespace I4Twins.Readings.Infrastructure.Persistence;

public sealed class ReadingEntity
{
    public long Id { get; set; }

    public string DeviceId { get; set; } = string.Empty;

    public string Metric { get; set; } = string.Empty;

    public long TimestampUtcTicks { get; set; }

    public double Value { get; set; }

    public int Seq { get; set; }

    public static ReadingEntity FromDomain(Reading reading)
    {
        ArgumentNullException.ThrowIfNull(reading);

        return new ReadingEntity
        {
            DeviceId = reading.DeviceId,
            Metric = reading.Metric,
            TimestampUtcTicks = reading.TimestampUtc.UtcTicks,
            Value = reading.Value,
            Seq = reading.Seq
        };
    }

    public Reading ToDomain()
    {
        var timestampUtc = new DateTimeOffset(TimestampUtcTicks, TimeSpan.Zero);

        var validationResult = Reading.Create(
            DeviceId,
            Metric,
            timestampUtc.ToString("O", CultureInfo.InvariantCulture),
            Value,
            Seq);

        if (!validationResult.IsValid)
        {
            throw new InvalidOperationException(
                $"Stored reading is invalid: {string.Join("; ", validationResult.Errors)}");
        }

        return validationResult.Reading!;
    }
}