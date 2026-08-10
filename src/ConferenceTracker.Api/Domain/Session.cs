namespace ConferenceTracker.Api.Domain;

public class Session
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Abstract { get; set; }
    public SessionLevel Level { get; set; }
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public Guid SpeakerId { get; set; }
    public Speaker Speaker { get; set; } = null!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = null!;

    public ICollection<Registration> Registrations { get; set; } = [];
}
