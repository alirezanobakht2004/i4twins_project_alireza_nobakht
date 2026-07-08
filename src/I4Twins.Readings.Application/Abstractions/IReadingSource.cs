using I4Twins.Readings.Application.Ingestion;

namespace I4Twins.Readings.Application.Abstractions;

public interface IReadingSource
{
    IAsyncEnumerable<RawReadingRecord> ReadAsync(
        CancellationToken cancellationToken = default);
}