namespace I4Twins.Readings.Domain.Readings;

public sealed class ReadingDeduplicator
{
    public ReadingDeduplicationResult Deduplicate(IEnumerable<Reading> readings)
    {
        ArgumentNullException.ThrowIfNull(readings);

        var uniqueByIdentity = new Dictionary<ReadingIdentity, Reading>();

        var duplicatesRemoved = 0;
        var conflictingDuplicates = 0;

        foreach (var reading in readings)
        {
            if (!uniqueByIdentity.TryAdd(reading.Identity, reading))
            {
                duplicatesRemoved++;

                var alreadyStoredReading = uniqueByIdentity[reading.Identity];

                if (!alreadyStoredReading.Value.Equals(reading.Value))
                {
                    conflictingDuplicates++;
                }
            }
        }

        return new ReadingDeduplicationResult(
            uniqueByIdentity.Values.ToList(),
            duplicatesRemoved,
            conflictingDuplicates);
    }
}