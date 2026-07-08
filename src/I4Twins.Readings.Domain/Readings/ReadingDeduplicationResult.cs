namespace I4Twins.Readings.Domain.Readings;

public sealed class ReadingDeduplicationResult
{
    public ReadingDeduplicationResult(
        IReadOnlyList<Reading> uniqueReadings,
        int duplicatesRemoved,
        int conflictingDuplicates)
    {
        UniqueReadings = uniqueReadings;
        DuplicatesRemoved = duplicatesRemoved;
        ConflictingDuplicates = conflictingDuplicates;
    }

    public IReadOnlyList<Reading> UniqueReadings { get; }

    public int DuplicatesRemoved { get; }

    public int ConflictingDuplicates { get; }
}