# Conference Tracker

A small .NET 9 + EF Core demo API. Sessions happen in rooms, are given by speakers, and attendees
register for them — a room's capacity is the one real business rule, which keeps the API demo from
being pure CRUD.

## Run it

```bash
docker compose up -d                 # Postgres 17 on localhost:5433
dotnet run --project src/ConferenceTracker.Api
```

Migrations are applied and the database is seeded automatically on startup (Development only).
Then open <http://localhost:5179> — `/` redirects to the Scalar API reference at `/scalar/v1`.
The raw OpenAPI document is at `/openapi/v1.json`.

To reset everything: `docker compose down -v` and run again.

## Model

```
Speaker ──1:N── Session ──N:1── Room
                   │
                   N:M via Registration (RegisteredAt, Status)
                   │
                Attendee
```

## What's worth pointing at

| Feature | Where |
|---|---|
| Owned type — `ContactInfo` maps to `Contact_Email` / `Contact_Twitter` **on the Speakers table** | `Data/Configurations/SpeakerConfiguration.cs` |
| Many-to-many **with payload** and a composite key | `Domain/Registration.cs`, `RegistrationConfiguration.cs` |
| Enums stored as readable text, not ints | `.HasConversion<string>()` in the session/registration configs |
| Composable filters — nothing runs until `ToListAsync` | `Endpoints/SessionEndpoints.cs`, `GET /api/sessions` |
| Projection to DTOs, so SQL only selects the needed columns | every endpoint's `.Select(...)` |
| `DeleteBehavior.Restrict` vs `Cascade` | deleting a booked speaker fails; deleting a session takes its registrations with it |
| Deterministic seeding | `Data/DbSeeder.cs` |

Set `Microsoft.EntityFrameworkCore.Database.Command` to `Information` (already on in
`appsettings.Development.json`) to watch the generated SQL scroll past while clicking around.

## Demo script

The seed data is deterministic, so these ids always work. *Ask Me Anything: EF Core* sits in the
8-seat **Fireside Corner** with 7 people already registered — one seat left.

```bash
B=http://localhost:5179
S=0c000000-0000-0000-0000-000000000005   # Ask Me Anything: EF Core
A8=0d000000-0000-0000-0000-000000000008  # Hannes Pichler
A9=0d000000-0000-0000-0000-000000000009  # Ines Berger

curl -s $B/api/sessions/$S                                  # seatsLeft: 1

curl -s -X POST $B/api/sessions/$S/registrations \
     -H 'Content-Type: application/json' -d "{\"attendeeId\":\"$A8\"}"   # 201, room now full

curl -s -X POST $B/api/sessions/$S/registrations \
     -H 'Content-Type: application/json' -d "{\"attendeeId\":\"$A9\"}"   # 409 "Session is full"

curl -s -X DELETE $B/api/sessions/$S/registrations/$A8       # 204, seat freed
curl -s -X POST $B/api/sessions/$S/registrations \
     -H 'Content-Type: application/json' -d "{\"attendeeId\":\"$A8\"}"   # 201, reuses the existing row

curl -s "$B/api/sessions?level=Advanced"                     # filtered listing
curl -s $B/api/attendees/0d000000-0000-0000-0000-000000000001/agenda
```

Look at the tables directly:

```bash
docker compose exec db psql -U conference -d conference -c '\d "Speakers"'
docker compose exec db psql -U conference -d conference -c 'select "Title","Level" from "Sessions"'
```

## Endpoints

| Method | Route |
|---|---|
| GET, POST | `/api/rooms` |
| GET, POST | `/api/speakers` |
| GET, PUT, DELETE | `/api/speakers/{id}` |
| GET, POST | `/api/sessions` — filters: `?level=`, `?roomId=`, `?day=`, `?skip=`, `?take=` |
| GET, PUT, DELETE | `/api/sessions/{id}` |
| GET, POST | `/api/attendees` |
| GET | `/api/attendees/{id}/agenda` |
| GET, POST | `/api/sessions/{sessionId}/registrations` |
| DELETE | `/api/sessions/{sessionId}/registrations/{attendeeId}` |

## Notes

- `global.json` pins the SDK to 9.0.3xx — without it, an installed .NET 10 SDK would target `net10.0`.
- `dotnet-ef` is a local tool: `dotnet tool restore`, then
  `dotnet ef migrations add <Name> -p src/ConferenceTracker.Api`.
- The capacity check is a plain `CountAsync` in the same request — two concurrent registrations
  could both slip through. Deliberate: it's a talking point, not a booking system.
