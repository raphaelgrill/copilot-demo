using ConferenceTracker.Api.Contracts;
using ConferenceTracker.Api.Data;
using ConferenceTracker.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ConferenceTracker.Api.Endpoints;

public static class SpeakerEndpoints
{
    public static void MapSpeakerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/speakers").WithTags("Speakers");

        group.MapGet("/", async (ConferenceDbContext db) =>
            await db.Speakers
                .AsNoTracking()
                .OrderBy(s => s.FullName)
                .Select(s => new SpeakerResponse(s.Id, s.FullName, s.Bio, s.Contact.Email, s.Contact.Twitter))
                .ToListAsync())
            .WithSummary("List all speakers");

        group.MapGet("/{id:guid}", async Task<Results<Ok<SpeakerDetailResponse>, NotFound>> (
            Guid id, ConferenceDbContext db) =>
        {
            var speaker = await db.Speakers
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new SpeakerDetailResponse(
                    s.Id,
                    s.FullName,
                    s.Bio,
                    s.Contact.Email,
                    s.Contact.Twitter,
                    s.Sessions
                        .OrderBy(session => session.StartsAt)
                        .Select(session => new SpeakerSessionResponse(session.Id, session.Title, session.StartsAt))
                        .ToList()))
                .FirstOrDefaultAsync();

            return speaker is null ? TypedResults.NotFound() : TypedResults.Ok(speaker);
        })
            .WithSummary("Get a speaker including their sessions");

        group.MapPost("/", async Task<Created<SpeakerResponse>> (
            SaveSpeakerRequest request, ConferenceDbContext db) =>
        {
            var speaker = new Speaker
            {
                Id = Guid.CreateVersion7(),
                FullName = request.FullName,
                Bio = request.Bio,
                Contact = new ContactInfo(request.Email, request.Twitter)
            };

            db.Speakers.Add(speaker);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/api/speakers/{speaker.Id}", ToResponse(speaker));
        })
            .WithSummary("Create a speaker");

        group.MapPut("/{id:guid}", async Task<Results<Ok<SpeakerResponse>, NotFound>> (
            Guid id, SaveSpeakerRequest request, ConferenceDbContext db) =>
        {
            var speaker = await db.Speakers.FindAsync(id);
            if (speaker is null) return TypedResults.NotFound();

            speaker.FullName = request.FullName;
            speaker.Bio = request.Bio;
            speaker.Contact = new ContactInfo(request.Email, request.Twitter);
            await db.SaveChangesAsync();

            return TypedResults.Ok(ToResponse(speaker));
        })
            .WithSummary("Update a speaker");

        group.MapDelete("/{id:guid}", async Task<Results<NoContent, NotFound, Conflict<string>>> (
            Guid id, ConferenceDbContext db) =>
        {
            var speaker = await db.Speakers.FindAsync(id);
            if (speaker is null) return TypedResults.NotFound();

            if (await db.Sessions.AnyAsync(s => s.SpeakerId == id))
                return TypedResults.Conflict("Speaker still has sessions scheduled.");

            db.Speakers.Remove(speaker);
            await db.SaveChangesAsync();

            return TypedResults.NoContent();
        })
            .WithSummary("Delete a speaker");
    }

    private static SpeakerResponse ToResponse(Speaker speaker) =>
        new(speaker.Id, speaker.FullName, speaker.Bio, speaker.Contact.Email, speaker.Contact.Twitter);
}
