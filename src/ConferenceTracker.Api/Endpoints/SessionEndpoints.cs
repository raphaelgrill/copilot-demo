using ConferenceTracker.Api.Contracts;
using ConferenceTracker.Api.Data;
using ConferenceTracker.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ConferenceTracker.Api.Endpoints;

public static class SessionEndpoints
{
    public static void MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions").WithTags("Sessions");

        group.MapGet("/", async (
            ConferenceDbContext db,
            SessionLevel? level,
            Guid? roomId,
            DateOnly? day,
            int skip = 0,
            int take = 50) =>
        {
            // Nothing executes until ToListAsync — the filters just compose the expression tree.
            var query = db.Sessions.AsNoTracking();

            if (level is not null)
                query = query.Where(s => s.Level == level);

            if (roomId is not null)
                query = query.Where(s => s.RoomId == roomId);

            if (day is not null)
            {
                var from = new DateTimeOffset(day.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
                var to = from.AddDays(1);
                query = query.Where(s => s.StartsAt >= from && s.StartsAt < to);
            }

            return await query
                .OrderBy(s => s.StartsAt)
                .Skip(skip)
                .Take(Math.Clamp(take, 1, 200))
                .Select(s => new SessionListItemResponse(
                    s.Id, s.Title, s.Level, s.StartsAt, s.EndsAt, s.Speaker.FullName, s.Room.Name))
                .ToListAsync();
        })
            .WithSummary("List sessions, optionally filtered by level, room or day");

        group.MapGet("/{id:guid}", async Task<Results<Ok<SessionDetailResponse>, NotFound>> (
            Guid id, ConferenceDbContext db) =>
        {
            var session = await db.Sessions
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new
                {
                    s.Id,
                    s.Title,
                    s.Abstract,
                    s.Level,
                    s.StartsAt,
                    s.EndsAt,
                    SpeakerName = s.Speaker.FullName,
                    RoomName = s.Room.Name,
                    s.Room.Capacity,
                    RegisteredCount = s.Registrations.Count(r => r.Status == RegistrationStatus.Confirmed)
                })
                .FirstOrDefaultAsync();

            if (session is null) return TypedResults.NotFound();

            return TypedResults.Ok(new SessionDetailResponse(
                session.Id,
                session.Title,
                session.Abstract,
                session.Level,
                session.StartsAt,
                session.EndsAt,
                session.SpeakerName,
                session.RoomName,
                session.Capacity,
                session.RegisteredCount,
                session.Capacity - session.RegisteredCount));
        })
            .WithSummary("Get a session including how many seats are left");

        group.MapPost("/", async Task<Results<Created<SessionListItemResponse>, ValidationProblem>> (
            SaveSessionRequest request, ConferenceDbContext db) =>
        {
            var problems = await ValidateAsync(request, db);
            if (problems.Count > 0) return TypedResults.ValidationProblem(problems);

            var session = new Session
            {
                Id = Guid.CreateVersion7(),
                Title = request.Title,
                Abstract = request.Abstract,
                Level = request.Level,
                StartsAt = request.StartsAt,
                EndsAt = request.EndsAt,
                SpeakerId = request.SpeakerId,
                RoomId = request.RoomId
            };

            db.Sessions.Add(session);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/api/sessions/{session.Id}", await ToListItemAsync(session.Id, db));
        })
            .WithSummary("Create a session");

        group.MapPut("/{id:guid}", async Task<Results<Ok<SessionListItemResponse>, NotFound, ValidationProblem>> (
            Guid id, SaveSessionRequest request, ConferenceDbContext db) =>
        {
            var session = await db.Sessions.FindAsync(id);
            if (session is null) return TypedResults.NotFound();

            var problems = await ValidateAsync(request, db);
            if (problems.Count > 0) return TypedResults.ValidationProblem(problems);

            session.Title = request.Title;
            session.Abstract = request.Abstract;
            session.Level = request.Level;
            session.StartsAt = request.StartsAt;
            session.EndsAt = request.EndsAt;
            session.SpeakerId = request.SpeakerId;
            session.RoomId = request.RoomId;
            await db.SaveChangesAsync();

            return TypedResults.Ok(await ToListItemAsync(session.Id, db));
        })
            .WithSummary("Update a session");

        group.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound>> (
            Guid id, ConferenceDbContext db) =>
        {
            var session = await db.Sessions.FindAsync(id);
            if (session is null) return TypedResults.NotFound();

            // Registrations cascade away with the session.
            db.Sessions.Remove(session);
            await db.SaveChangesAsync();

            return TypedResults.NoContent();
        })
            .WithSummary("Delete a session and its registrations");
    }

    private static async Task<Dictionary<string, string[]>> ValidateAsync(SaveSessionRequest request, ConferenceDbContext db)
    {
        var problems = new Dictionary<string, string[]>();

        if (request.EndsAt <= request.StartsAt)
            problems[nameof(request.EndsAt)] = ["EndsAt must be after StartsAt."];

        if (!await db.Speakers.AnyAsync(s => s.Id == request.SpeakerId))
            problems[nameof(request.SpeakerId)] = ["Speaker does not exist."];

        if (!await db.Rooms.AnyAsync(r => r.Id == request.RoomId))
            problems[nameof(request.RoomId)] = ["Room does not exist."];

        return problems;
    }

    private static async Task<SessionListItemResponse> ToListItemAsync(Guid id, ConferenceDbContext db) =>
        await db.Sessions
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new SessionListItemResponse(
                s.Id, s.Title, s.Level, s.StartsAt, s.EndsAt, s.Speaker.FullName, s.Room.Name))
            .FirstAsync();
}
