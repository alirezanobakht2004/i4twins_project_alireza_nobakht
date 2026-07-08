namespace I4Twins.Readings.Application.Abstractions;

public sealed record StoreReadingResult(StoreReadingStatus Status)
{
    public static StoreReadingResult Stored()
    {
        return new StoreReadingResult(StoreReadingStatus.Stored);
    }

    public static StoreReadingResult Duplicate()
    {
        return new StoreReadingResult(StoreReadingStatus.Duplicate);
    }

    public static StoreReadingResult ConflictingDuplicate()
    {
        return new StoreReadingResult(StoreReadingStatus.ConflictingDuplicate);
    }
}