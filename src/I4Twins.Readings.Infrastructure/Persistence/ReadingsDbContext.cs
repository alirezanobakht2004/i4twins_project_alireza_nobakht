using Microsoft.EntityFrameworkCore;

namespace I4Twins.Readings.Infrastructure.Persistence;

public sealed class ReadingsDbContext : DbContext
{
    public ReadingsDbContext(DbContextOptions<ReadingsDbContext> options)
        : base(options)
    {
    }

    public DbSet<ReadingEntity> Readings => Set<ReadingEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reading = modelBuilder.Entity<ReadingEntity>();

        reading.ToTable("Readings");

        reading.HasKey(x => x.Id);

        reading.Property(x => x.DeviceId)
            .HasMaxLength(100)
            .IsRequired();

        reading.Property(x => x.Metric)
            .HasMaxLength(100)
            .IsRequired();

        reading.Property(x => x.TimestampUtcTicks)
            .IsRequired();

        reading.Property(x => x.Value)
            .IsRequired();

        reading.Property(x => x.Seq)
            .IsRequired();

        reading.HasIndex(x => new
        {
            x.DeviceId,
            x.Metric,
            x.TimestampUtcTicks,
            x.Seq
        }).IsUnique();

        reading.HasIndex(x => new
        {
            x.DeviceId,
            x.Metric,
            x.TimestampUtcTicks
        });
    }
}