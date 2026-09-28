# Test fixtures

Recorded data so downstream phases can be tested without calling an external provider and
without re-reading the commissioner's workbook. `docs/swan-league-spec.md` is the rule
authority; these files are the data authority.

All of it is **real 2026-season data**, recorded 2026-09-28 (during week 3 of the 2026 regular
season). Nothing here is synthesised.

## Layout

| File | Consumer |
|---|---|
| `teams-2026.json` | Phase 1 — `Team` / `PointTier` |
| `roster-2026.json` | Phase 1 — `SeasonRoster`; Phase 3 — bye and collision handling |
| `odds-espn-2026-week3.json` | Phase 2 — `ILinesProvider` adapter |
| `scores-espn-2026-week1.json` | Phase 2 — `IResultsProvider` adapter (all final) |
| `scores-espn-2026-week3.json` | Phase 2 — `IResultsProvider` adapter (one game still scheduled) |
| `expected/lines-2026-week3.json` | Phase 2 — expected normalized adapter output |
| `expected/scoring-2026-workbook-cases.json` | Phase 1 — `IScoringService` golden cases |
| `expected/recommendation-2026-week3.json` | Phase 3 — `IStarterRecommendationService` acceptance case |
| `expected/dog-recommendation-2026-week3.json` | Phase 3 — `IDogRecommendationService` acceptance case |

`expected/` holds derived values. The files above it hold recorded source data. If a `expected/`
file and its source disagree, the source wins and the expected file needs regenerating.

## Provenance

**Workbook-derived** (`docs/2026-27 NFL Draft Sheet.xlsx`) — point values, tier colours, the
coach's roster, and every golden scoring case. Each value carries its cell reference in the
fixture's own `source` field, so it can be re-verified without guesswork.

**Provider-derived** — free ESPN endpoints, no API key:

- scores: `https://site.api.espn.com/apis/site/v2/sports/football/nfl/scoreboard?dates=2026&seasontype=2&week={week}`
- odds: `https://sports.core.api.espn.com/v2/sports/football/leagues/nfl/events/{eventId}/competitions/{competitionId}/odds`

The recorded payloads are stored under a `response` key (per game, for odds), wrapped in a small
metadata envelope. The response bodies are **re-indented for reviewability but otherwise
unmodified** — no field dropped, added, renamed or retyped. Adapters should be tested against
this full shape, including the fields they don't use, not against a hand-trimmed subset.

> **Provider choice is a Phase 2 deliverable, not settled here.** These fixtures exist so Phase 2
> has a realistic shape to build against on day one. If Phase 2 selects a different odds provider
> or scores feed, re-record against the chosen provider and keep these as the ESPN-shaped case —
> do not delete them, and do not let the rest of the suite depend on ESPN's field names outside
> the adapter.

**Schedule-derived** — bye weeks and intra-roster collisions are computed from all 18 weeks of the
real 2026 schedule rather than copied from `docs/Swan League — Rules.md`, which has both wrong.
See `docs/swan-league-spec.md` §10. Validation applied: 32 teams, exactly one bye each, 32 byes
total.

## Re-recording

The fixtures are static on purpose — nothing in CI refreshes them. To re-record:

1. Re-run the endpoints above for the week you want; keep the response bodies verbatim.
2. Regenerate `expected/lines-*.json` from the new odds payload (favourite side, underdog spread,
   `ceil(|spread|)` dog points).
3. Regenerate `expected/recommendation-*.json` and `expected/dog-recommendation-*.json` with the
   σ and `erf` approximation pinned in `docs/swan-league-spec.md` §7. **Do not change σ to make a
   number look nicer** — σ is a spec pin, confirmed by the board on 2026-09-28, and moving it is a
   spec revision.
4. Leave the workbook-derived files alone unless the workbook itself changed. If it did, update
   the cell references too.

## Known edge cases these fixtures deliberately cover

- `scores-espn-2026-week3.json` — `PHI @ CHI` is `STATUS_SCHEDULED`. A week with an unplayed game
  is incomplete, not a set of losses.
- `odds-espn-2026-week3.json` — `BAL VS DAL` is a neutral-site game (`neutralSite: true`), so
  home/away is not a reliable proxy for anything.
- `expected/lines-2026-week3.json` — 6 of 16 games have the **away** team favoured, which catches
  adapters that assume the home side is the favourite.
- Half-point spreads (`-3.5`, `-7.5`, `-8.5`, `-2.5`, `-1.5`, `-4.5`) and whole-number spreads
  (`-7`, `-10`, `-3`) both appear, so the `ceil` rule is exercised in both directions.
- `expected/dog-recommendation-2026-week3.json` — the biggest spread on the board (`MIA +10`)
  ranks **third**, and `+2.5` outranks `+3` on identical points. Both catch a dog ranker that
  sorts by spread instead of by EV.
- `expected/scoring-2026-workbook-cases.json` — `samt-week-1-perfect-but-not-going-perf` is a
  Perfect Week (+3, 3 starters) that is **not** a Going Perf (the dog lost). That distinction is
  the single easiest thing to get wrong in the scoring engine.
- `teams-2026.json → workbookAliases` — 38 distinct spellings for 32 teams, including
  `Cincinatti Bengals` and `Detriot Lions`. Any workbook parser needs the alias list.
