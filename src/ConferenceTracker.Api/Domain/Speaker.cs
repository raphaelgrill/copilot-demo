namespace ConferenceTracker.Api.Domain;

/// <summary>Owned value object — persisted into the Speakers table, not a table of its own.</summary>
public record ContactInfo(string Email, string? Twitter);

public class Speaker
{
    public Guid Id { get; set; }
    public required string FullName { get; set; }
    public required string Bio { get; set; }
    public required ContactInfo Contact { get; set; }

    public ICollection<Session> Sessions { get; set; } = [];
}
