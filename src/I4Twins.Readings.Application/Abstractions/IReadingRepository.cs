using I4Twins.Readings.Domain.Readings;

namespace I4Twins.Readings.Application.Abstractions;

public interface IReadingRepository
{
    Task<StoreReadingResult> StoreIfNotExistsAsync(
        Reading reading,
        CancellationToken cancellationToken = default);
}