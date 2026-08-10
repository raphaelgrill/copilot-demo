using ConferenceTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceTracker.Api.Data.Configurations;

public class SpeakerConfiguration : IEntityTypeConfiguration<Speaker>
{
    public void Configure(EntityTypeBuilder<Speaker> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.FullName).HasMaxLength(150).IsRequired();
        builder.Property(s => s.Bio).HasMaxLength(1000).IsRequired();

        // Owned type: no Contacts table, the columns live on Speakers.
        builder.OwnsOne(s => s.Contact, contact =>
        {
            contact.Property(c => c.Email).HasColumnName("Contact_Email").HasMaxLength(200).IsRequired();
            contact.Property(c => c.Twitter).HasColumnName("Contact_Twitter").HasMaxLength(50);
        });
    }
}
