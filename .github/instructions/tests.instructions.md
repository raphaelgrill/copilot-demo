---
description: 'Testkonventionen — xUnit gegen echtes Postgres'
applyTo: 'tests/**/*.cs'
---

# Tests

## Namen und Gliederung

- **Testmethoden heißen deutsch, in `Pascal_Case_mit_Unterstrichen`** und beschreiben einen
  ganzen Satz: `Anmeldung_auf_den_letzten_Platz_gelingt`,
  `Anmeldung_zu_einer_vollen_Sitzung_wird_abgelehnt`. Umlaute ausgeschrieben.
- **Jeder Test ist mit drei Kommentarzeilen gegliedert:** `// Vorbereiten`, `// Ausfuehren`,
  `// Pruefen`. Auch dann, wenn ein Abschnitt nur eine Zeile hat.
- Ein Verhalten pro Test.

## Aufbau

- **xUnit**, `[Fact]` bzw. `[Theory]` mit `[InlineData]`. Kein NUnit, kein MSTest.
- **Kein Mocking-Framework.** Kein Moq, kein NSubstitute. Was ein Double braucht, wird als
  kleine Klasse von Hand geschrieben.
- **Niemals `UseInMemoryDatabase`.** Der In-Memory-Provider kennt weder den zusammengesetzten
  Schlüssel von `Registration` noch `DeleteBehavior.Restrict` noch die Enum-Konversion — ein Test,
  der dort grün ist, sagt über Postgres nichts aus.
- API-Tests laufen über `ConferenceApiFactory` und liegen in einer Klasse mit
  `[Collection(PostgresCollection.Name)]`. Reine Domain-Tests brauchen die Factory nicht.
- Ausgangsdaten kommen aus `DbSeeder`. Die Ids dort sind deterministisch — vorhandene Entities
  werden über ihre Seed-Id als `private static readonly Guid` angesprochen, nicht über ein
  frisches `Guid.NewGuid()`.

## Assertions

- Geprüft wird **Statuscode und Payload** — der Response wird in das Record aus `Contracts`
  deserialisiert und auf konkrete Werte geprüft, nicht auf „nicht null".
- `Assert.Equal(erwartet, tatsaechlich)` — Reihenfolge einhalten.
- Durchgehend `await`; kein `.Result`, kein `.Wait()`, kein `Thread.Sleep`.
