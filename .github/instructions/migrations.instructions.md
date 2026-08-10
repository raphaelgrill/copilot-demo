---
description: 'EF-Core-Migrationen — generiert, nicht handgeschrieben'
applyTo: 'src/**/Migrations/*.cs'
---

# Migrationen

Diese Dateien sind generiert und werden **nicht** von Hand bearbeitet. Ist etwas daran falsch,
wird die Migration verworfen (`dotnet ef migrations remove -p src/ConferenceTracker.Api`), die
Konfiguration in `Data/Configurations/` korrigiert und neu generiert.

Bereits ausgerollte Migrationen werden nie geändert — Korrekturen kommen als neue Migration.
