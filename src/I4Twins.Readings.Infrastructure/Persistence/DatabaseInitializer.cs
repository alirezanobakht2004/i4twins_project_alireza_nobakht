using Microsoft.EntityFrameworkCore;

namespace I4Twins.Readings.Infrastructure.Persistence;

public sealed class DatabaseInitializer
{
    private readonly ReadingsDbContext _dbContext;

    public DatabaseInitializer(ReadingsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}