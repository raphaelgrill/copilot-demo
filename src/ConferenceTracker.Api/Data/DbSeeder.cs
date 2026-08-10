using ConferenceTracker.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace ConferenceTracker.Api.Data;

/// <summary>
/// Deterministic seed data — fixed ids and fixed dates, so the ids in the README stay valid
/// across a `docker compose down -v` and the demo script is reproducible.
/// </summary>
public static class DbSeeder
{
    private static readonly DateTimeOffset Day1 = new(2026, 9, 15, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Day2 = new(2026, 9, 16, 0, 0, 0, TimeSpan.Zero);

    private static Guid RoomId(int n) => new($"0a000000-0000-0000-0000-{n:D12}");
    private static Guid SpeakerId(int n) => new($"0b000000-0000-0000-0000-{n:D12}");
    private static Guid SessionId(int n) => new($"0c000000-0000-0000-0000-{n:D12}");
    private static Guid AttendeeId(int n) => new($"0d000000-0000-0000-0000-{n:D12}");

    public static async Task SeedAsync(ConferenceDbContext db)
    {
        if (await db.Rooms.AnyAsync()) return;

        db.Rooms.AddRange(
            new Room { Id = RoomId(1), Name = "Main Hall", Capacity = 50 },
            new Room { Id = RoomId(2), Name = "Workshop Room", Capacity = 20 },
            new Room { Id = RoomId(3), Name = "Fireside Corner", Capacity = 8 });

        db.Speakers.AddRange(
            new Speaker
            {
                Id = SpeakerId(1),
                FullName = "Mara Lindqvist",
                Bio = "Backend developer, spends far too much time reading generated SQL.",
                Contact = new ContactInfo("mara@example.com", "@maralq")
            },
            new Speaker
            {
                Id = SpeakerId(2),
                FullName = "Tobias Renner",
                Bio = "Database consultant. Believes every N+1 problem is a teachable moment.",
                Contact = new ContactInfo("tobias@example.com", "@trenner")
            },
            new Speaker
            {
                Id = SpeakerId(3),
                FullName = "Priya Nandakumar",
                Bio = "Performance engineer working on high-throughput .NET services.",
                Contact = new ContactInfo("priya@example.com", null)
            },
            new Speaker
            {
                Id = SpeakerId(4),
                FullName = "Jonas Weber",
                Bio = "Platform engineer, migration survivor, reluctant YAML expert.",
                Contact = new ContactInfo("jonas@example.com", "@jweber")
            },
            new Speaker
            {
                Id = SpeakerId(5),
                FullName = "Elena Costa",
                Bio = "Postgres advocate and long-time application developer.",
                Contact = new ContactInfo("elena@example.com", "@elenac")
            });

        db.Sessions.AddRange(
            NewSession(1, "Minimal APIs in .NET 9", "Routing, binding and typed results without a single controller.",
                SessionLevel.Beginner, Day1.AddHours(9), Day1.AddHours(10), 1, 1),
            NewSession(2, "EF Core Query Internals", "From expression tree to SQL, and what gets lost on the way.",
                SessionLevel.Advanced, Day1.AddHours(10.5), Day1.AddHours(11.5), 2, 1),
            NewSession(3, "Owned Types and Value Objects", "Modelling value objects that map onto the parent table.",
                SessionLevel.Intermediate, Day1.AddHours(10.5), Day1.AddHours(11.5), 3, 2),
            NewSession(4, "Migrations Without Fear", "Reviewing, editing and shipping migrations to production.",
                SessionLevel.Intermediate, Day1.AddHours(13), Day1.AddHours(14), 4, 2),
            NewSession(5, "Ask Me Anything: EF Core", "Small-room session. Bring your worst query.",
                SessionLevel.Advanced, Day1.AddHours(15), Day1.AddHours(16), 2, 3),
            NewSession(6, "Postgres Tricks for .NET Developers", "Indexes, JSONB and the features Npgsql exposes.",
                SessionLevel.Intermediate, Day2.AddHours(9), Day2.AddHours(10), 5, 1),
            NewSession(7, "Testing Data Access", "Where the in-memory provider lies to you, and what to do instead.",
                SessionLevel.Beginner, Day2.AddHours(10.5), Day2.AddHours(11.5), 1, 2),
            NewSession(8, "Performance Deep Dive", "Tracking, split queries and the cost of doing nothing.",
                SessionLevel.Advanced, Day2.AddHours(13), Day2.AddHours(14.5), 3, 1));

        string[] names =
        [
            "Anna Bauer", "Ben Huber", "Clara Moser", "David Gruber",
            "Eva Steiner", "Felix Wagner", "Greta Hofer", "Hannes Pichler",
            "Ines Berger", "Jakob Winkler", "Klara Fuchs", "Lukas Mayr"
        ];

        for (var i = 0; i < names.Length; i++)
        {
            db.Attendees.Add(new Attendee
            {
                Id = AttendeeId(i + 1),
                FullName = names[i],
                Email = $"{names[i].Split(' ')[0].ToLowerInvariant()}.{names[i].Split(' ')[1].ToLowerInvariant()}@example.com"
            });
        }

        // Session 5 sits in the 8-seat Fireside Corner and gets 7 confirmed registrations,
        // so the capacity rule is exactly one seat away from firing.
        AddRegistrations(db, session: 5, attendees: [1, 2, 3, 4, 5, 6, 7]);
        AddRegistrations(db, session: 1, attendees: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10]);
        AddRegistrations(db, session: 2, attendees: [1, 3, 5, 7, 9, 11]);
        AddRegistrations(db, session: 3, attendees: [4, 5, 6, 12]);
        AddRegistrations(db, session: 4, attendees: [2, 8, 10]);
        AddRegistrations(db, session: 6, attendees: [1, 2, 3, 11, 12]);
        AddRegistrations(db, session: 7, attendees: [6, 7, 8]);
        AddRegistrations(db, session: 8, attendees: [1, 9, 10, 11]);

        await db.SaveChangesAsync();
    }

    private static Session NewSession(
        int id, string title, string @abstract, SessionLevel level,
        DateTimeOffset startsAt, DateTimeOffset endsAt, int speaker, int room) =>
        new()
        {
            Id = SessionId(id),
            Title = title,
            Abstract = @abstract,
            Level = level,
            StartsAt = startsAt,
            EndsAt = endsAt,
            SpeakerId = SpeakerId(speaker),
            RoomId = RoomId(room)
        };

    private static void AddRegistrations(ConferenceDbContext db, int session, int[] attendees)
    {
        foreach (var attendee in attendees)
        {
            db.Registrations.Add(new Registration
            {
                SessionId = SessionId(session),
                AttendeeId = AttendeeId(attendee),
                RegisteredAt = Day1.AddDays(-30).AddMinutes(session * 100 + attendee),
                Status = RegistrationStatus.Confirmed
            });
        }
    }
}
