using I4Twins.Readings.Application.Abstractions;
using I4Twins.Readings.Domain.Readings;

namespace I4Twins.Readings.Application.Ingestion;

public sealed class IngestReadingsService
{
    private readonly IReadingSource _readingSource;
    private readonly IReadingRepository _readingRepository;
    private readonly ReadingDeduplicator _deduplicator;

    public IngestReadingsService(
        IReadingSource readingSource,
        IReadingRepository readingRepository,
        ReadingDeduplicator deduplicator)
    {
        _readingSource = readingSource;
        _readingRepository = readingRepository;
        _deduplicator = deduplicator;
    }

    public async Task<IngestionReport> IngestAsync(
        CancellationToken cancellationToken = default)
    {
        var totalLines = 0;
        var invalidRecordsRejected = 0;
        var validReadings = new List<Reading>();

        await foreach (var rawRecord in _readingSource.ReadAsync(cancellationToken))
        {
            totalLines++;

            if (rawRecord.HasParseError)
            {
                invalidRecordsRejected++;
                continue;
            }

            var validationResult = Reading.Create(
                rawRecord.DeviceId,
                rawRecord.Metric,
                rawRecord.Timestamp,
                rawRecord.Value,
                rawRecord.Seq);

            if (!validationResult.IsValid)
            {
                invalidRecordsRejected++;
                continue;
            }

            validReadings.Add(validationResult.Reading!);
        }

        var deduplicationResult = _deduplicator.Deduplicate(validReadings);

        var storedReadings = 0;
        var databaseDuplicates = 0;
        var databaseConflictingDuplicates = 0;

        foreach (var reading in deduplicationResult.UniqueReadings)
        {
            var storeResult = await _readingRepository.StoreIfNotExistsAsync(
                reading,
                cancellationToken);

            switch (storeResult.Status)
            {
                case StoreReadingStatus.Stored:
                    storedReadings++;
                    break;

                case StoreReadingStatus.Duplicate:
                    databaseDuplicates++;
                    break;

                case StoreReadingStatus.ConflictingDuplicate:
                    databaseDuplicates++;
                    databaseConflictingDuplicates++;
                    break;

                default:
                    throw new InvalidOperationException(
                        $"Unsupported store result: {storeResult.Status}");
            }
        }

        return new IngestionReport(
            TotalLines: totalLines,
            StoredReadings: storedReadings,
            DuplicatesRemoved: deduplicationResult.DuplicatesRemoved + databaseDuplicates,
            InvalidRecordsRejected: invalidRecordsRejected,
            ConflictingDuplicates: deduplicationResult.ConflictingDuplicates + databaseConflictingDuplicates);
    }
}