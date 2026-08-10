namespace ConferenceTracker.Api.Domain;

/// <summary>
/// Many-to-many join entity *with payload* — Attendee &lt;-&gt; Session plus when they signed up
/// and whether the registration still stands.
/// </summary>
public class Registration
{
    public Guid SessionId { get; set; }
    public Session Session { get; set; } = null!;

    public Guid AttendeeId { get; set; }
    public Attendee Attendee { get; set; } = null!;

    public DateTimeOffset RegisteredAt { get; set; }
    public RegistrationStatus Status { get; set; }
}
