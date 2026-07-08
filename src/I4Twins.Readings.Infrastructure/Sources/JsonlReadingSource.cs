using System.Runtime.CompilerServices;
using System.Text.Json;
using I4Twins.Readings.Application.Abstractions;
using I4Twins.Readings.Application.Ingestion;

namespace I4Twins.Readings.Infrastructure.Sources;

public sealed class JsonlReadingSource : IReadingSource
{
    private readonly string _filePath;

    public JsonlReadingSource(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        _filePath = filePath;
    }

    public async IAsyncEnumerable<RawReadingRecord> ReadAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Readings file was not found.", _filePath);
        }

        using var reader = new StreamReader(_filePath);

        var lineNumber = 0;

        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lineNumber++;

            var line = await reader.ReadLineAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(line))
            {
                yield return new RawReadingRecord(
                    LineNumber: lineNumber,
                    DeviceId: null,
                    Metric: null,
                    Timestamp: null,
                    Value: null,
                    Seq: null,
                    ParseError: "Line is empty.");

                continue;
            }

            RawReadingRecord rawRecord;

            try
            {
                rawRecord = ParseLine(line, lineNumber);
            }
            catch (JsonException ex)
            {
                rawRecord = new RawReadingRecord(
                    LineNumber: lineNumber,
                    DeviceId: null,
                    Metric: null,
                    Timestamp: null,
                    Value: null,
                    Seq: null,
                    ParseError: $"Invalid JSON: {ex.Message}");
            }

            yield return rawRecord;
        }
    }

    private static RawReadingRecord ParseLine(string line, int lineNumber)
    {
        using var document = JsonDocument.Parse(line);

        var root = document.RootElement;

        return new RawReadingRecord(
            LineNumber: lineNumber,
            DeviceId: GetStringOrNull(root, "deviceId"),
            Metric: GetStringOrNull(root, "metric"),
            Timestamp: GetStringOrNull(root, "ts"),
            Value: GetDoubleOrNull(root, "value"),
            Seq: GetIntOrNull(root, "seq"));
    }

    private static string? GetStringOrNull(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static double? GetDoubleOrNull(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var value)
            ? value
            : null;
    }

    private static int? GetIntOrNull(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var value)
            ? value
            : null;
    }
}