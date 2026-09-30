# Swan League Personal Tracker + UI Redesign

> **Superseded as a scoring reference.** `docs/swan-league-spec.md` is authoritative for all
> scoring. The "Resolved rules" section below is wrong on item 6 — Going Perf / 0fer are a
> max/min point total, not a count of occurrences (spec §8). The plan's Part 1 / Part 2 shape and
> the data-source open question are also out of date; the phase breakdown on NEX-2 supersedes
> them. Kept for the UI direction in Part 2.

## Context

This app is **not** the league's system of record — the commissioner runs Swan League
themselves via a Google Form + master spreadsheet (`docs/2026-27 NFL Draft Sheet.xlsx`).
That format is theirs; this plan does not touch it, replace it, or try to become an
admin tool for the whole league. This app is the user's **personal companion tool**:
track their own 9-team roster, get a data-driven weekly-starter recommendation (per
`docs/Swan League — Weekly Starter Method.md`), log the 3 starters + dog pick they
actually submitted to the commissioner, and track their own running score against
the rules in `docs/Swan League — Rules.md`. It is single-user scope, not multi-coach
league administration.

The current codebase (`src/AgainstTheSpread.Core`, `.Functions`, `.Web`) is hardcoded to
a different, unrelated format entirely — against-the-spread confidence-pool picks with
Excel export, no League/scoring concept, no persistence of picks server-side. That old
format is being fully replaced (not kept alongside) with the personal-tracker model
below. Alongside the model conversion, the UI gets a modern/minimal redesign with a
Sleeper-app-inspired, gamified feel.

## Resolved rules (confirmed with user / cross-referenced against the commissioner's sheet)

Only needed for scoring the user's own picks correctly — not for running the league.

1. **Dog points** = the underdog's spread, rounded up (e.g. Browns +6 → 6 pts; a `+6.5`
   line rounds up to 7, per the sheet's FAQ).
2. **Dog points count toward both** the Main pool total and the separate Dog pool total.
3. A team **can** be both a weekly starter and the dog pick in the same week.
4. **Color tiers**: red = 1, yellow = 2, blue = 3, green = 4 (point value ascending).
5. **#1 seed / conference-winner Wild Card bye**: flat **+10 points** to any coach
   rostering that team (confirmed from the sheet's Rules tab).
6. **Going Perf / 0fer**: awarded for **most occurrences** over the season, tracked
   here only for the user's own weeks (not league-wide).

## Part 1 — Domain model conversion (personal scope)

### New Core models (`src/AgainstTheSpread.Core/Models/`)

Replace `Game.cs`, `WeeklyLines.cs`, `UserPicks.cs`, `BowlPick.cs`, `BowlUserPicks.cs`,
`BowlLines.cs`, `BowlGame.cs` entirely — none of the bowl/spread-confidence concepts
carry over.

- `Team.cs` — NFL team + 2025 division finish → `PointValue` (1–4) + tier color derived
  from the resolved mapping.
- `MyRoster.cs` — the user's own 9 drafted teams for the season. Single instance per
  season, not a multi-coach collection.
- `WeeklyGame.cs` — one game: teams, moneyline/spread (for dog-point calc and the EV
  starter method), and a `Result` once known. Entered manually by the user or fetched
  from a odds/scores source (see open question below) — not uploaded by an admin for a
  whole league.
- `WeeklyPick.cs` — the user's own submission for a week: `StartedTeamIds` (exactly 3,
  validated against `MyRoster` + bye weeks), `DogPicks` (1, or 2 on double-dog weeks).
  This is what the user actually sent to the commissioner — the app logs it, it doesn't
  submit it anywhere.
- `PlayoffPick.cs` — playoff-round extension: multiplier (x2/x4/x6/x8) + 3-pt qualify +
  10-pt bye bonus, for the user's rostered teams only.

### Scoring logic (new — personal, not league-wide)

`Services/IScoringService.cs` + `ScoringService.cs` in Core:
- Weekly score = started-team points (win=PointValue, loss/tie=0) + dog points (spread
  rounded up, if won) + 3 if all 3 starters won (Perfect Week; dog excluded).
- Running season total across the 4 pool categories (Main/Dog/Regular/Playoff), purely
  as the user's own tally — not a league leaderboard.
- The user's own Going Perf / 0fer week detection.
- Playoff scoring: point value × round multiplier, +3 qualify, +10 bye.

### Weekly starter recommendation (new — this is the main personal value-add)

Implements `docs/Swan League — Weekly Starter Method.md` directly:
`Services/IStarterRecommendationService.cs` — moneyline → implied win % → de-vig →
EV = win% × point value → rank; tiebreak within ~0.05 EV via perfect-week probability
(product of the three win%'s). Surfaces the top-3 recommendation plus the reasoning
(EV numbers, tiebreak trigger) so the user can sanity-check before submitting picks
themselves via the commissioner's Google Form.

### Storage

Keep the existing blob-storage approach (`IStorageService`/`StorageService.cs`), scoped
to one user's data: roster (season-start), weekly games/odds/results (user-entered or
fetched), weekly picks (user-logged). No admin/upload-for-others surface needed —
retire the admin-broadcast upload model tied to the old lines format.

### Functions (`src/AgainstTheSpread.Functions/`)

- Retire all Bowl* functions and the old `PicksFunction`/`LinesFunction`/
  `UploadLinesFunction` shape (Excel-export model).
- `GamesFunction` — get/set weekly games + results (personal data entry, or a fetch
  from an odds source — open question, see below).
- `PicksFunction` — log the user's own weekly starters + dog pick, validated against
  roster/byes; persisted so score history can be computed.
- `RecommendationFunction` — GET the EV-ranked starter suggestion for a given week.
- `MyStandingsFunction` — GET the user's own running totals across the 4 pool
  categories + Going Perf/0fer count.
- Auth (`AdminMeFunction`/`Authentication/`) simplifies to "is this the one user" rather
  than a league-admin role, or can be dropped if this becomes single-tenant with no
  shared deployment concern — confirm with user before removing auth entirely.

### Tests

Rewrite `Models/` tests for the new entities; add `Services/ScoringServiceTests.cs` and
`Services/StarterRecommendationServiceTests.cs` (EV ranking + tiebreak is the highest-
value new logic to cover); rewrite `Functions/` tests for the new endpoints; drop all
Bowl-specific and Excel-export test files.

## Open question to resolve before/at start of implementation

Where do weekly moneylines/spreads and game results come from? Options: (a) the user
enters them manually each week (simplest, no new dependency), (b) fetch from a sports-
odds API (adds an external dependency + API key management). Given this is a personal
tool, manual entry is the lower-risk default — flag this as a decision to confirm, not
assumed.

## Part 2 — UI redesign

### Direction

Modern/minimal, Sleeper-app-inspired: dark-mode-first, near-black/navy surfaces, one
saturated accent color for primary actions, and the rules' own red/yellow/blue/green
point-tier colors reused as first-class UI tokens (team point-value chips) — a natural
gamification hook already in the rules. Card-based layout, bold numerals for point
values/EV, badge-style treatment for the user's own Going Perf/0fer weeks, clean
leaderboard-style display of the user's running totals (even though it's solo data,
presented as a personal "stat line," not a form).

### Scope

- Replace Bootstrap-default `app.css` with a small custom design-token layer (CSS
  variables for the dark palette + tier colors); keep Bootstrap grid/utilities where
  useful, override component-level styling — no new component library needed.
- Rework `Picks.razor` into: this week's EV-ranked recommendation → user logs their
  actual 3 starters + dog pick → running score. Point-value chips colored by tier.
- New `MyStats.razor` (or similar) — the user's own season totals across the 4 pool
  categories, Going Perf/0fer tally, simple week-by-week history.
- Rework or retire `Admin.razor` — no more league-wide upload; may shrink to "enter
  this week's games/results" if manual entry is chosen in Part 1.
- Drop `BowlPicks.razor`, `Counter.razor`, `Weather.razor` (dead scaffolding).
- Keep `Components/TeamLogo.razor` + `TeamColorService`/`TeamColors.cs`, recolor
  surrounding chrome only.

### Tests

Rewrite `Web/Pages/PicksDownloadFlowTests` for the new log-picks flow (no more Excel
download as the primary path — confirm whether Excel export still has any value for a
personal tool, likely not). Drop `BowlPicksDownloadFlowTests`. Add a bUnit test for the
new stats/recommendation page.

## Sequencing

Part 1 (domain/services) first with green `dotnet test`, then Part 2 (UI) against the
new services. Given the size, two PRs rather than one: (1) domain/scoring/recommendation
conversion, (2) UI redesign — each independently reviewable, each verified per
CLAUDE.md's four-step process (build+test → local E2E → PR → preview-slot browser
check, read-only only, since this is single-user data anyway).

## Verification

- `dotnet build` + `dotnet test AgainstTheSpread.sln` clean after each part.
- Full local E2E (`task start-e2e` → Playwright → `task stop-e2e`) once the new
  Picks/Recommendation/Stats pages exist against the real Functions host + Azurite.
- PR preview slot: browser-check the redesigned pages before merging deploy-affecting
  changes.
