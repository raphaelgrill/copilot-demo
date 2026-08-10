# Conference Tracker

Minimal-API über EF Core auf Postgres. Sessions finden in Räumen statt, werden von Speakern
gehalten, Attendees registrieren sich. Die Raumkapazität ist die einzige echte Geschäftsregel.

## Aufbau

- `src/…/Endpoints/` — je Ressource eine `static class` mit einer `Map…Endpoints()`-Extension,
  registriert in `Program.cs`. **Minimal APIs, keine Controller.**
- `src/…/Contracts/Contracts.cs` — alle Request-/Response-`record`s.
- `src/…/Domain/` — Entities. `src/…/Data/Configurations/` — je Entity eine
  `IEntityTypeConfiguration<T>`, keine Data Annotations.
- `tests/ConferenceTracker.Api.Tests/` — xUnit gegen echtes Postgres via Testcontainers.

## Regeln

- **Domain-Entities verlassen die API-Grenze nie.** Jede Query endet auf `.Select(...)` in ein
  Record aus `Contracts` — nicht auf `.Include(...)` mit anschließender Handbefüllung.
- Lesende Queries laufen mit `AsNoTracking()`.
- Rückgabetyp ist die Union `Task<Results<Ok<T>, NotFound, …>>` mit `TypedResults.*` —
  nicht `IResult` mit `Results.*`.
- Validierung ist eine private `ValidateAsync`-Methode in derselben Endpoint-Klasse und liefert
  `TypedResults.ValidationProblem`.
- Neue Ids: `Guid.CreateVersion7()`.
- Neue Enums werden als Text persistiert — `.HasConversion<string>()` in der Konfiguration.
- **Jede Änderung an `Domain/` oder `Data/Configurations/` braucht im selben Schritt eine
  Migration:** `dotnet tool restore` und
  `dotnet ef migrations add <Name> -p src/ConferenceTracker.Api`.

## Schreibweise

- **Lokale Variablen und Methodenparameter werden deutsch benannt** — `antwort`, `sitzung`,
  `bestaetigteAnmeldungen`. Umlaute ausgeschrieben (`ae`, `oe`, `ue`).
- **Typen, Properties, Records und öffentliche Methoden bleiben englisch** — die sind Teil der
  API-Oberfläche und stehen im OpenAPI-Dokument.
- **Kommentare im Code sind deutsch.**

## Commands

- `docker compose up -d` — Postgres 17 auf `localhost:5433`
- `dotnet run --project src/ConferenceTracker.Api` — migriert und seedet beim Start (Development)
- `dotnet test` — braucht einen laufenden Docker-Daemon
- API-Referenz auf <http://localhost:5179/scalar/v1>

## Nicht tun

- **Keine neuen NuGet-Pakete ohne Rückfrage** — insbesondere kein FluentValidation, AutoMapper,
  MediatR. Was hier fehlt, fehlt absichtlich.
- Migrationsdateien nicht von Hand bearbeiten, kein `EnsureCreated()`.
- Keine Connection-Strings im Code.
