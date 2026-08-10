using ConferenceTracker.Api.Contracts;
using ConferenceTracker.Api.Data;
using ConferenceTracker.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ConferenceTracker.Api.Endpoints;

public static class RegistrationEndpoints
{
    public static void MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions/{sessionId:guid}/registrations").WithTags("Registrations");

        group.MapGet("/", async Task<Results<Ok<List<RegistrationResponse>>, NotFound>> (
            Guid sessionId, ConferenceDbContext db) =>
        {
            if (!await db.Sessions.AnyAsync(s => s.Id == sessionId)) return TypedResults.NotFound();

            var registrations = await db.Registrations
                .AsNoTracking()
                .Where(r => r.SessionId == sessionId)
                .OrderBy(r => r.RegisteredAt)
                .Select(r => new RegistrationResponse(r.SessionId, r.AttendeeId, r.RegisteredAt, r.Status))
                .ToListAsync();

            return TypedResults.Ok(registrations);
        })
            .WithSummary("List registrations for a session");

        group.MapPost("/", async Task<Results<Created<RegistrationResponse>, NotFound<string>, Conflict<string>>> (
            Guid sessionId, CreateRegistrationRequest request, ConferenceDbContext db) =>
        {
            var session = await db.Sessions
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session is null)
                return TypedResults.NotFound($"Session {sessionId} does not exist.");

            if (!await db.Attendees.AnyAsync(a => a.Id == request.AttendeeId))
                return TypedResults.NotFound($"Attendee {request.AttendeeId} does not exist.");

            var existing = await db.Registrations
                .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.AttendeeId == request.AttendeeId);

            if (existing is { Status: RegistrationStatus.Confirmed })
                return TypedResults.Conflict("Attendee is already registered for this session.");

            // The business rule worth demoing: a session cannot outgrow its room.
            var confirmed = await db.Registrations
                .CountAsync(r => r.SessionId == sessionId && r.Status == RegistrationStatus.Confirmed);

            if (confirmed >= session.Room.Capacity)
                return TypedResults.Conflict($"Session is full ({session.Room.Capacity} seats in {session.Room.Name}).");

            if (existing is null)
            {
                existing = new Registration
                {
                    SessionId = sessionId,
                    AttendeeId = request.AttendeeId,
                    RegisteredAt = DateTimeOffset.UtcNow,
                    Status = RegistrationStatus.Confirmed
                };
                db.Registrations.Add(existing);
            }
            else
            {
                // Re-registering after a cancellation reuses the existing row.
                existing.Status = RegistrationStatus.Confirmed;
                existing.RegisteredAt = DateTimeOffset.UtcNow;
            }

            await db.SaveChangesAsync();

            return TypedResults.Created(
                $"/api/sessions/{sessionId}/registrations/{request.AttendeeId}",
                new RegistrationResponse(sessionId, request.AttendeeId, existing.RegisteredAt, existing.Status));
        })
            .WithSummary("Register an attendee for a session (409 when the room is full)");

        group.MapDelete("/{attendeeId:guid}", async Task<Results<NoContent, NotFound>> (
            Guid sessionId, Guid attendeeId, ConferenceDbContext db) =>
        {
            var registration = await db.Registrations
                .FirstOrDefaultAsync(r => r.SessionId == sessionId && r.AttendeeId == attendeeId);

            if (registration is null) return TypedResults.NotFound();

            registration.Status = RegistrationStatus.Cancelled;
            await db.SaveChangesAsync();

            return TypedResults.NoContent();
        })
            .WithSummary("Cancel a registration and free the seat");
    }
}
