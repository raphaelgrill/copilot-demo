# Review-Regeln

Gilt nur für Copilot code review am Pull Request.

**Jeder Kommentar beginnt mit `🔎 Review-Regel:`.** Ohne dieses Präfix keinen Kommentar abgeben.

## Worauf geschaut wird — in dieser Reihenfolge

1. **Persistenz-Korrektheit.** Ändert der PR etwas an `Domain/` oder `Data/Configurations/`,
   ohne dass eine Migration unter `Migrations/` dabei ist, ist das ein Blocker. Ebenso eine von
   Hand bearbeitete Migration und ein neues Enum ohne `.HasConversion<string>()`.
2. **Query-Verhalten.** Lesende Query ohne `AsNoTracking()`. Query, die eine Entity statt eines
   Records aus `Contracts` zurückgibt. `Include(...)`, dessen Ergebnis anschließend ohnehin
   projiziert wird. Query in einer Schleife.
3. **HTTP-Semantik.** Statuscode passend zur Operation (201 mit Location bei Create, 204 bei
   Delete, 409 bei Konflikt, 404 statt leerer 200). Rückgabetyp als `Results<…>`-Union.
4. **Datenlecks.** Felder in einer Response, die dort fachlich nichts verloren haben.

## Was nicht kommentiert wird

- **Die Kapazitätsprüfung in `RegistrationEndpoints` ist bekannt und so gewollt.** Count und
  Insert liegen auseinander, zwei gleichzeitige Anfragen können beide durchrutschen. Das ist im
  README dokumentiert und ein Gesprächsthema, kein Findings-Kandidat — nicht in jedem PR erneut
  melden.
- Fehlende Authentifizierung, Autorisierung, Rate Limiting. Die API ist absichtlich offen.
- Formatierung, Namensgeschmack, `var` vs. expliziter Typ, Kommentardichte.
- Bestätigende Kommentare ohne Handlungsbedarf.

## Form

Inline kommentiert wird nur, was Korrektheit oder Daten betrifft. Alles Übrige gehört in die
Zusammenfassung. Jeder Kommentar nennt die konkrete Stelle und den Vorschlag — keine Rückfragen
an den Autor.
