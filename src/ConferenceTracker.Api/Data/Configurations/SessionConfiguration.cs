using ConferenceTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceTracker.Api.Data.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Title).HasMaxLength(200).IsRequired();
        builder.Property(s => s.Abstract).HasMaxLength(2000).IsRequired();

        // Stored as text ("Advanced") instead of an int, so the raw rows stay readable.
        builder.Property(s => s.Level).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.StartsAt);

        builder.HasOne(s => s.Speaker)
            .WithMany(sp => sp.Sessions)
            .HasForeignKey(s => s.SpeakerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Room)
            .WithMany(r => r.Sessions)
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
