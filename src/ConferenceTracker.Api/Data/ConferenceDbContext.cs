using ConferenceTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConferenceTracker.Api.Data;

public class ConferenceDbContext(DbContextOptions<ConferenceDbContext> options) : DbContext(options)
{
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Speaker> Speakers => Set<Speaker>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Attendee> Attendees => Set<Attendee>();
    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ConferenceDbContext).Assembly);
    }
}
