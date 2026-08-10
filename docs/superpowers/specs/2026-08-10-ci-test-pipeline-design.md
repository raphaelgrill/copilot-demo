# CI pipeline that runs the tests

## Problem

`post-demo-dotnet` is a .NET 9 demo API (`src/ConferenceTracker.Api`) backed by Postgres. There is
no `.github/` directory and no test project, so "a pipeline which runs the tests" needs both halves:
a test project with real tests, and a GitHub Actions workflow that runs it.

The one real business rule in the app is the room-capacity check in
`src/ConferenceTracker.Api/Endpoints/RegistrationEndpoints.cs` (`POST /api/sessions/{id}/registrations`
returns 409 once confirmed registrations reach `session.Room.Capacity`). That rule lives inside the
HTTP handler and depends on EF Core, so it is only meaningfully testable end-to-end against a real
Postgres.

## Approach

Integration tests driven through `WebApplicationFactory`, with Postgres supplied by
Testcontainers for .NET. The same code path runs locally and in CI — no `services:` block, no
`docker compose up` prerequisite. `ubuntu-latest` runners ship with Docker, so Testcontainers works
out of the box.

The workflow stays minimal: restore, build, test.

## Design

### Test project

`tests/ConferenceTracker.Api.Tests/ConferenceTracker.Api.Tests.csproj`, xUnit, `net9.0`, added to
`ConferenceTracker.sln`.

Packages:

- `Microsoft.NET.Test.Sdk`, `xunit`, `xunit.runner.visualstudio`
- `Microsoft.AspNetCore.Mvc.Testing` (9.0.x, matching the API's 9.0.18 pins)
- `Testcontainers.PostgreSql`

Plus a `ProjectReference` to `src/ConferenceTracker.Api/ConferenceTracker.Api.csproj`.

### API changes (deliberately minimal)

1. Append `public partial class Program;` to `Program.cs`. Top-level statements generate an
   `internal` `Program` class; `WebApplicationFactory<Program>` needs it visible. (Alternative:
   `InternalsVisibleTo`. The partial class is the more conventional choice.)
2. **No change** to the `IsDevelopment()` guard around `MigrateAsync` / `DbSeeder.SeedAsync`.
   The test fixture runs migrations and seeding itself against the container database, so
   production startup behaviour is untouched.

### Fixture

- `PostgresFixture` — an xUnit **collection** fixture owning a single `PostgreSqlContainer`
  (`postgres:17-alpine`, matching `docker-compose.yml`) started once per test run. One container
  start is the dominant cost; sharing it keeps the suite fast.
- `ConferenceApiFactory : WebApplicationFactory<Program>` — overrides the `Conference` connection
  string via `ConfigureAppConfiguration` in-memory settings so the app points at the container.
- Per-test-class isolation: each class gets a **fresh database** on the shared container
  (`CREATE DATABASE conference_<guid>`), then `MigrateAsync` + `DbSeeder.SeedAsync`. This keeps the
  deterministic seed ids from the README valid in every test without cross-test bleed, and avoids
  needing a separate respawn/cleanup library.

### Tests

`RegistrationCapacityTests` — the room-capacity rule, driven over HTTP against the seeded
*Ask Me Anything: EF Core* session (`0c00…0005`) in the 8-seat *Fireside Corner*, which starts with
7 confirmed registrations:

| Scenario | Expected |
|---|---|
| Register attendee 8 (`0d00…0008`) | 201 Created, `seatsLeft` now 0 |
| Register attendee 9 (`0d00…0009`) into the now-full room | 409, body mentions "Session is full" |
| Cancel attendee 8's registration | 204, seat freed |
| Re-register attendee 8 after cancelling | 201, reuses the existing row (registration count unchanged) |
| Register an already-confirmed attendee again | 409 "already registered" |
| Register into an unknown session id | 404 |
| Register an unknown attendee id | 404 |

The seed baseline (7 of 8 seats taken) is asserted at the start rather than assumed, so a change to
`DbSeeder` fails loudly instead of silently weakening the tests.

### Workflow

`.github/workflows/ci.yml`:

- Triggers: `push` to `main`, and `pull_request`.
- `runs-on: ubuntu-latest`, `concurrency` group cancelling superseded runs on the same ref.
- Steps: `actions/checkout` → `actions/setup-dotnet` with `global-json-file: global.json` →
  `dotnet restore` → `dotnet build --no-restore --configuration Release` →
  `dotnet test --no-build --configuration Release`.
- No `services:` block and no NuGet cache complexity — keeping it minimal was the explicit choice.

### README

Add a short "Tests" section: `dotnet test` (requires Docker running), and a note that CI runs the
same command.

## Todos

1. Add the xUnit test project under `tests/` and register it in `ConferenceTracker.sln`.
2. Append `public partial class Program;` to `Program.cs`.
3. Implement `PostgresFixture` (shared container) and `ConferenceApiFactory` (connection-string
   override + per-class fresh, migrated, seeded database).
4. Write `RegistrationCapacityTests` covering the table above.
5. Verify locally: `dotnet test` green with Docker running.
6. Add `.github/workflows/ci.yml`.
7. Validate the workflow YAML (`gh workflow view` after push, or `actionlint` if available).
8. Update `README.md` with the Tests section.

## Notes and considerations

- **Docker is a hard prerequisite** for running the tests locally. If that's unacceptable later,
  the fallback is the GitHub Actions `services: postgres` route, which was the option not chosen.
- **Package versions**: the API pins EF Core / ASP.NET packages at 9.0.18. `Microsoft.AspNetCore.Mvc.Testing`
  must be on the same 9.0.x band to avoid assembly-version conflicts.
- **`bin/` and `obj/` are present** in `src/`; confirm `.gitignore` covers the new `tests/` tree too
  (it uses the standard patterns, so it should).
- The capacity check is deliberately racy (a plain `CountAsync` — see the README's Notes). The tests
  document the single-request behaviour only; **no concurrency test**, since the known race would
  make it flaky and the raciness is an intentional talking point.
- Per the brainstorming skill, the approved design should also be committed to
  `docs/superpowers/specs/2026-08-10-ci-test-pipeline-design.md` as the first implementation step —
  plan mode cannot write outside the session folder.
