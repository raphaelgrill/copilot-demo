# Room Schedule Endpoint — Design

**Date:** 2026-08-11
**Status:** Approved

## Goal

Expose `GET /api/rooms/{id}/schedule` so a client can retrieve the sessions of one
room on one day, in chronological order, with the idle time between each session
and the next one in the same room.

## Route

```
GET /api/rooms/{id:guid}/schedule?day=2026-09-15
```

Registered in `RoomEndpoints.MapRoomEndpoints`, inside the existing
`/api/rooms` group, so it inherits the `Rooms` OpenAPI tag.

`day` is a required `DateOnly` query parameter. It is interpreted as a UTC day,
matching the existing `day` filter on `GET /api/sessions`.

## Contracts

Two records are added to `Contracts.cs` under the Rooms section:

```csharp
public record ScheduleItemResponse(
    Guid SessionId,
    string Title,
    SessionLevel Level,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string SpeakerName,
    int? GapToNextMinutes);

public record RoomScheduleResponse(
    Guid RoomId,
    string RoomName,
    DateOnly Day,
    IReadOnlyList<ScheduleItemResponse> Items);
```

## Behaviour

| Case | Result |
| --- | --- |
| Room does not exist | `404 Not Found` |
| `day` missing or unparseable | `400` validation problem |
| Room exists, no sessions that day | `200` with an empty `Items` list |
| Room exists, sessions that day | `200` with items ordered by `StartsAt` |

**Day window.** `[day 00:00:00 UTC, day + 1 day)`, filtered on `StartsAt`. This
mirrors the window used by `GET /api/sessions`, so a session that starts before
midnight and ends after it belongs to the day it starts on.

**Ordering.** `StartsAt`, then `EndsAt` as a tiebreaker, so two sessions that
start at the same instant come back in a deterministic order.

**Idle time.** `GapToNextMinutes` is the number of whole minutes from a
session's `EndsAt` to the next session's `StartsAt`.

- The last session of the day has no successor, so its value is `null`.
- Overlapping sessions produce a negative value. The domain does not prevent
  double-booking a room, and a negative gap surfaces the conflict rather than
  hiding it.
- Truncation toward zero is acceptable: seeded and realistic schedule data uses
  whole minutes.

## Implementation

A single `AsNoTracking` query loads the day's sessions for the room, projected
to the fields the response needs (including `Speaker.FullName`, so there is no
N+1). The gaps are then computed in one pass over the ordered list:

```
for i in 0..n-1:
    gap[i] = (starts[i+1] - ends[i]).TotalMinutes   // null when i is the last
```

The result set is one room-day, at most a few dozen rows, so in-memory
computation is cheaper and clearer than a SQL window function. EF Core cannot
express `LEAD` in LINQ, so the alternative would require raw SQL and would break
with the LINQ style used throughout the project.

The endpoint returns
`Results<Ok<RoomScheduleResponse>, NotFound, ValidationProblem>`.

The room lookup fetches `Id` and `Name` in its own query; that doubles as the
existence check, so no extra `AnyAsync` round trip is needed.

## Testing

A new `RoomScheduleTests.cs` in `tests/ConferenceTracker.Api.Tests`, following
the pattern of `RegistrationCapacityTests`: `PostgresCollection`,
`ConferenceApiFactory`, seeded data.

The seed gives Main Hall on day 1 (2026-09-15) two sessions — "Minimal APIs in
.NET 9" 09:00–10:00 and "EF Core Query Internals" 10:30–11:30 — a 30-minute gap.

Cases:

1. Main Hall, day 1 — two items, chronological, first gap `30`.
2. Main Hall, day 1 — the last item's gap is `null`.
3. Fireside Corner, day 2 (2026-09-16) — `200` with an empty `Items` list.
4. Unknown room id — `404`.
5. `day` omitted — `400`.
6. Response echoes `RoomId`, `RoomName` and `Day`.
