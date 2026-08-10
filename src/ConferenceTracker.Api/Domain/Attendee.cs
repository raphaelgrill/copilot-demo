namespace ConferenceTracker.Api.Domain;

public class Attendee
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }

    public ICollection<Registration> Registrations { get; set; } = [];
}
