using ConferenceTracker.Api.Contracts;
using ConferenceTracker.Api.Data;
using ConferenceTracker.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace ConferenceTracker.Api.Endpoints;

public static class RoomEndpoints
{
    public static void MapRoomEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rooms").WithTags("Rooms");

        group.MapGet("/", async (ConferenceDbContext db) =>
            await db.Rooms
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .Select(r => new RoomResponse(r.Id, r.Name, r.Capacity))
                .ToListAsync())
            .WithSummary("List all rooms");

        group.MapPost("/", async Task<Created<RoomResponse>> (CreateRoomRequest request, ConferenceDbContext db) =>
        {
            var room = new Room
            {
                Id = Guid.CreateVersion7(),
                Name = request.Name,
                Capacity = request.Capacity
            };

            db.Rooms.Add(room);
            await db.SaveChangesAsync();

            return TypedResults.Created($"/api/rooms/{room.Id}",
                new RoomResponse(room.Id, room.Name, room.Capacity));
        })
            .WithSummary("Create a room");
    }
}
