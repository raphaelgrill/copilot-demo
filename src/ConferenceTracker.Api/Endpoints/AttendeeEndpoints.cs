using ConferenceTracker.Api.Contracts;
using ConferenceTracker.Api.Data;
using ConferenceTracker.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ConferenceTracker.Api.Endpoints;

public static class AttendeeEndpoints
{
    public static void MapAttendeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendees").WithTags("Attendees");

        group.MapGet("/", async (ConferenceDbContext db) =>
            await db.Attendees
                .AsNoTracking()
                .OrderBy(a => a.FullName)
                .Select(a => new AttendeeResponse(a.Id, a.FullName, a.Email))
                .ToListAsync())
            .WithSummary("List all attendees");

        group.MapPost("/", async Task<Results<Created<AttendeeResponse>, Conflict<string>>> (
            CreateAttendeeRequest request, ConferenceDbContext db) =>
        {
            if (await db.Attendees.AnyAsync(a => a.Email == request.Email))
                return TypedResults.Conflict($"An attendee with email {request.Email} already exists.");

            var attendee = new Attendee
            {
                Id = Guid.CreateVersion7(),
                FullName = request.FullName,
                Email = request.Email
            };

            db.Attendees.Add(attendee);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/api/attendees/{attendee.Id}",
                new AttendeeResponse(attendee.Id, attendee.FullName, attendee.Email));
        })
            .WithSummary("Register a new attendee");

        group.MapGet("/{id:guid}/agenda", async Task<Results<Ok<List<AgendaItemResponse>>, NotFound>> (
            Guid id, ConferenceDbContext db) =>
        {
            if (!await db.Attendees.AnyAsync(a => a.Id == id)) return TypedResults.NotFound();

            var agenda = await db.Registrations
                .AsNoTracking()
                .Where(r => r.AttendeeId == id && r.Status == RegistrationStatus.Confirmed)
                .OrderBy(r => r.Session.StartsAt)
                .Select(r => new AgendaItemResponse(
                    r.SessionId,
                    r.Session.Title,
                    r.Session.StartsAt,
                    r.Session.EndsAt,
                    r.Session.Room.Name,
                    r.Session.Speaker.FullName))
                .ToListAsync();

            return TypedResults.Ok(agenda);
        })
            .WithSummary("Get an attendee's confirmed sessions in start order");

        group.MapGet("/{id:guid}/registrations", async Task<Results<Ok<List<Registration>>, NotFound>> (
            Guid id, ConferenceDbContext db) =>
        {
            var attendee = await db.Attendees.FirstOrDefaultAsync(a => a.Id == id);
            if (attendee is null) return TypedResults.NotFound();

            var registrations = await db.Registrations
                .Include(r => r.Session)
                .ThenInclude(s => s.Room)
                .Where(r => r.AttendeeId == id)
                .OrderByDescending(r => r.RegisteredAt)
                .ToListAsync();

            return TypedResults.Ok(registrations);
        })
            .WithSummary("List every registration of an attendee, cancelled ones included");
    }
}
