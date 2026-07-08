namespace I4Twins.Readings.Application.Ingestion;

public sealed record IngestionReport(
    int TotalLines,
    int StoredReadings,
    int DuplicatesRemoved,
    int InvalidRecordsRejected,
    int ConflictingDuplicates);