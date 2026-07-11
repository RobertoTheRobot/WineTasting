using Microsoft.EntityFrameworkCore;

namespace WineTasting.Data;

public class WineTastingDbContext : DbContext
{
    public WineTastingDbContext(DbContextOptions<WineTastingDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Wine> Wines => Set<Wine>();
    public DbSet<Participant> Participants => Set<Participant>();
    public DbSet<Rating> Ratings => Set<Rating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(e =>
        {
            e.HasMany(x => x.Wines)
                .WithOne(x => x.Event!)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Participants)
                .WithOne(x => x.Event!)
                .HasForeignKey(x => x.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Wine>(e =>
        {
            e.HasIndex(x => new { x.EventId, x.Number }).IsUnique();

            e.HasMany(x => x.Ratings)
                .WithOne(x => x.Wine!)
                .HasForeignKey(x => x.WineId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Participant>(e =>
        {
            e.HasMany(x => x.Ratings)
                .WithOne(x => x.Participant!)
                .HasForeignKey(x => x.ParticipantId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Rating>(e =>
        {
            e.HasIndex(x => new { x.ParticipantId, x.WineId }).IsUnique();
        });
    }
}
