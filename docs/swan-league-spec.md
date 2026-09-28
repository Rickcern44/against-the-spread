# Swan League — Scoring Specification

**Status: authoritative.** Sep 28, 2026.

This document is the single source of truth for Swan League scoring in this codebase. Where it
disagrees with `docs/Swan League — Rules.md`, `docs/Personal Tracker + UI Redesign Plan.md`, or
`docs/Swan League — Weekly Starter Method.md`, **this document wins** and the other document is
stale. Those three remain useful as narrative background; none of them should be read as a
scoring reference again.

Precedence used to settle every contradiction below: the commissioner's workbook
(`docs/2026-27 NFL Draft Sheet.xlsx`, `Rules` and `Scoreboard` tabs) is the highest authority,
because it is the league's actual system of record. Cell references in this document point at
that workbook so each decision can be re-verified rather than re-argued.

Scope reminder: this app is a **single-coach personal companion tool**, not league
administration. It scores the user's own picks. It does not run the league.

---

## 1. Point values and tiers

Every team carries a point value for the whole season, set by where it finished in its **2025**
division (`DraftCentral!B2:K6`).

| 2025 division finish | Points per win | Tier | Hex |
|---|---|---|---|
| 1st | 1 | Red | `#FF0000` |
| 2nd | 2 | Yellow | `#FFFF00` |
| 3rd | 3 | Blue | `#00FFFF` |
| 4th | 4 | Green | `#00FF00` |

**The tier colors are settled and must not be re-derived.** They are read directly off the fill
colors of the `Points per Win` cells `DraftCentral!K3:K6` — `K3` (1 pt) is `FFFF0000`, `K4`
(2 pts) is `FFFFFF00`, `K5` (3 pts) is `FF00FFFF`, `K6` (4 pts) is `FF00FF00`. This is a direct
source, stronger than the inference from the playoff tiebreaker example
(`Rules!C28`: "1 red, 3 yellows = 7 points" → 1 + 3×2 = 7), which agrees with it.

"Blue" in the rules text is cyan (`#00FFFF`) in the workbook, not a navy blue. Use the hex, not
the word, when these become UI tokens.

The inversion is the core mechanic: a 2025 last-place team that wins is worth 4× a 2025 division
winner that wins.

All 32 teams with finish, point value, tier and hex are committed in
`src/AgainstTheSpread.Tests/Fixtures/teams-2026.json`.

---

## 2. Weekly scoring

Each week the coach starts **exactly 3** of their 9 rostered teams.

```
starterPoints(week)   = Σ over the 3 started teams: won ? team.PointValue : 0
perfectWeekBonus(week)= (all 3 started teams won) ? 3 : 0
dogPoints(week)       = Σ over that week's dog picks: wonOutright ? ceil(|officialSpread|) : 0
```

- A **loss or a tie scores zero**. No partial credit, no margin of victory (`Rules!C35`: "Ties
  are not wins. No points.").
- The **Perfect Week +3 is on the 3 started teams only** — the dog is explicitly excluded
  (`Rules!C5`). A tie by any starter breaks the Perfect Week.
- A team on bye cannot be started. A started team's game not yet final leaves the week
  incomplete; it is not a loss.
- A team **may** be both a weekly starter and that week's dog pick. Both score independently.

### 2.1 Dog scoring

- The dog is **any team without the minus sign** on the posted line — not limited to the coach's
  own roster (`Rules!C6`, `Rules!C41`).
- The dog must **win outright**. Covering the spread is irrelevant; a tie is a loss.
- Points = the underdog's spread **rounded up (ceiling)**: `+6.5 → 7`, `+6 → 6`, `+7 → 7`
  (`Rules!C41`).
- Dog points count toward **both** the Main pool and the Dog pool, and toward **neither** the
  Regular Season pool nor the Playoff pool. Verified arithmetically: `Scoreboard!AN12`=22
  (Main) = `AP12`=19 (Regular Season) + `AO12`=3 (Dog); same identity holds for every coach row
  in `Scoreboard!AM3:AQ26`.
- The `Rules!C6` worked example ("Browns +6 in week one … beat the Steelers") is **illustrative
  only**. In the real 2026 week 1 the Browns lost at Jacksonville and the Steelers beat Atlanta.
  Do not use it as a fixture.
- There is **no eligibility filter on the dog**: no minimum or maximum spread, no once-per-season
  restriction, no roster requirement. The app must not impose one. Help with the choice is a
  **ranking**, not a filter — see §7.5.

### 2.2 Double-dog weeks are data, not constants

`Rules!C43` (FAQ) names Week 2, Thanksgiving week and Christmas week. `Rules!C7` says there will
be a **minimum of three double-dog weeks at the commissioner's discretion**. `Rules!C7` is the
operative rule; the FAQ is that season's expected instance of it.

**Therefore: model the dog allowance as a per-week integer on the week itself**, defaulting to 1,
settable to 2 (or more) by the user when the commissioner declares it. Do not hardcode
`week == 2 || isThanksgiving || isChristmas` anywhere — a fourth double-dog week declared in
November would silently misscore the rest of the season.

Validation rule: a week's dog picks must number exactly that week's declared allowance.

---

## 3. Official line vs. API line

Dog points are scored off **the commissioner's posted line**, which is a Tuesday snapshot that
can differ from any given market quote and from a later API pull.

`WeeklyGame` therefore carries **two** spreads:

| Field | Source | Used for |
|---|---|---|
| `ApiLine` | odds provider fetch | the starter recommendation, and as the pre-filled default |
| `OfficialLine` | the commissioner's posted line | **scoring**, authoritative |

- `OfficialLine` is nullable and manually settable (paste or sheet upload).
- When `OfficialLine` is null, the scoring engine falls back to `ApiLine` and **must flag the
  resulting dog points as unconfirmed**, so a divergence from the commissioner's sheet is
  visible rather than silent.
- Scoring never reads `ApiLine` when `OfficialLine` is set, even if `ApiLine` is newer.

---

## 4. Season shape

- **18 regular-season weeks**, plus **one combined `CC/P` column** for the
  conference-championship / playoff tally (`Scoreboard!D5:V5` — `D5`–`U5` are weeks 1–18, `V5` is
  `CC/P`, `W5` is `Total:`).
- Model the season as 18 week slots plus a single distinct playoff slot. Do not model weeks 19+.

---

## 5. The four pools

Four separate totals off a $100 buy-in (`Rules!C17:C29`). **They are not a partition of one
number** — the same points can land in more than one pool, and the playoff qualification bonus
lands in only one.

| Pool | Stake | What counts |
|---|---|---|
| Main | $60 | Regular-season points **+ dog points** (+ playoff points) |
| Dog | $20 | Dog points only |
| Regular Season | $10 | Starter points + Perfect Week bonuses. **Excludes dog points.** |
| Playoff | $10 | Playoff multiplier points + qualification bonuses + conference-winner bonuses |

### 5.1 Tiebreakers

- **Main:** win the Super Bowl → most teams in the playoffs → most playoff points.
- **Dog:** biggest margin of victory → second-biggest margin of victory.
- **Regular Season:** most wins → most **green** wins → most **blue** wins → most **yellow**
  wins → most **red** wins (`Rules!C25`).
- **Playoff:** win the Super Bowl → most teams in the playoffs → most combined points per team in
  the playoffs.

**Consequence for the model (this is a data-shape requirement, not a display concern):** the
Regular Season tiebreaker needs **wins bucketed by tier**, so a season tally must retain
`WinsByTier: { Green, Blue, Yellow, Red }` — a scalar win count is insufficient and cannot be
back-derived from a point total (2 green wins = 8 points, and so does 4 yellow wins).

The Dog tiebreaker needs the **margin of victory** of each winning dog pick retained, which means
final scores must be stored per dog pick, not just a win flag.

---

## 6. Playoffs

Multipliers apply to a team's regular point value (`Rules!C9:C12`):

| Round | Multiplier |
|---|---|
| Wild Card | ×2 |
| Divisional | ×4 |
| Conference | ×6 |
| Super Bowl | ×8 |

A 4-point team winning a Divisional game scores 16; a Super Bowl win scores 32.

- **+3 per rostered team just for qualifying** for the playoffs. `Rules!C13` is explicit that
  "these points go towards the playoff pool and **not** the regular season pool." The bonus
  therefore hits the Playoff pool and the Main pool, and never the Regular Season pool.
- **+10 flat for a conference winner** (the #1 seed that earns the Wild Card bye), paid to any
  coach rostering that team (`Rules!C14`). Flat — not multiplied, not scaled by point value.
- Playoff points do not receive a Perfect Week bonus.

---

## 7. Spread → win probability (pinned)

The recommendation engine converts the **spread**, not a moneyline. This function is pinned so
the engine has a reproducible expected output and Phase 3 can be unit-tested.

### 7.1 The function

For a team with expected margin `m` (positive when favored — a team laying 7.5 has `m = +7.5`;
its opponent has `m = -7.5`):

```
σ        = 13.5                       (pinned)
z        = m / σ
p_raw    = Φ(z)                       normal CDF
Φ(z)     = 0.5 × (1 + erf(z / √2))
```

Then normalize the pair to sum to 1:

```
p_team = p_raw(team) / ( p_raw(team) + p_raw(opponent) )
```

`erf` is pinned to **Abramowitz & Stegun 7.1.26** so the result is bit-reproducible across
platforms (.NET has no built-in `Erf`). For `x ≥ 0`:

```
t   = 1 / (1 + 0.3275911·x)
erf = 1 − (a₁t + a₂t² + a₃t³ + a₄t⁴ + a₅t⁵)·e^(−x²)

a₁ =  0.254829592
a₂ = −0.284496736
a₃ =  1.421413741
a₄ = −1.453152027
a₅ =  1.061405429
```

with `erf(−x) = −erf(x)`. Maximum absolute error 1.5×10⁻⁷.

**Assertions round probabilities and EVs to 4 decimal places.** The A&S error is two orders of
magnitude below that, so 4 dp is safe and any implementation of the above lands on the same
digits.

### 7.2 Why σ = 13.5

The standard deviation of NFL game margin around the closing spread sits in the 13.5–13.9 range
in published work. 13.5 is the round number at the bottom of that range. The choice is a pin, not
a claim of optimality — but it must not be changed casually, because every committed expected
value in `Fixtures/expected/recommendation-2026-week3.json` and
`Fixtures/expected/dog-recommendation-2026-week3.json` moves with it. Changing σ is a spec
revision plus a fixture regeneration, not a tuning knob.

**σ = 13.5 was raised as the spec's one judgment call and confirmed by the board on 2026-09-28.**
It is not a user setting and not a config knob. It affects suggestions only: nothing in §2
scoring reads a probability. Locked.

Sanity values: `Φ(0)=0.5`, `Φ(1)=0.841345`, `Φ(-1)=0.158655`.

### 7.3 De-vig is gone

There is **no separate de-vig step**. `docs/Swan League — Weekly Starter Method.md` step 2
("convert moneyline → implied win probability, then de-vig it") is superseded. A spread yields a
complementary probability pair by construction; the normalization in §7.1 is what de-vig used to
do.

Because Φ is symmetric, `p_raw(team) + p_raw(opponent)` is exactly 1 and the normalization is a
no-op at σ = 13.5. **Implement it anyway.** It is the contract, and it keeps the engine correct
if σ is ever made side-dependent (home-field, rest) or if a lookup table replaces Φ.

Edge cases:
- A pick-em game (`spread = 0`) gives both sides exactly 0.5.
- Ties are not modelled. The pair sums to 1 with no tie mass, which slightly overstates both win
  probabilities (NFL ties run ~0.2% of games). Accepted: the engine is a **ranking tool**, and a
  uniform ~0.1% overstatement on both sides cannot change an ordering.

### 7.4 The starter recommendation

```
EV(team) = p_team × team.PointValue
```

Rank all available (non-bye) rostered teams by EV descending; take the top 3. EV is a ranking
number, not a projected score — the actual week pays 0, 2, 3 or 4 per starter and nothing in
between.

**Tiebreak:** if the team holding the third slot and the next team are within **0.05 EV**, decide
between them on **perfect-week probability** — the product of all three prospective starters'
win probabilities — and give the slot to the higher product.

**Collisions:** when two rostered teams play each other, one is guaranteed to lose. Start the
side the line favors; never bench both.

### 7.5 The dog recommendation

Decided by the board on 2026-09-28: the app gives a **ranked dog list**, not a spread threshold.

```
EV(dog) = P(dog wins outright) × ceil(dogSpread)
```

- `P(dog wins outright)` is §7.1 evaluated at the dog's margin, i.e. `Φ(−dogSpread / σ)`. Same
  function, same σ, no second model.
- `ceil(dogSpread)` is the §2.1 points rule, so the ranking pays exactly what the week pays.
- **Scope is the whole slate** — every game's underdog, not just rostered teams (§2.1). A 16-game
  week produces 16 rows.
- Rank by EV descending. **Tiebreak:** equal EV → higher win probability → team abbreviation
  ascending. Deterministic, because exact EV ties are common (three of them in week 3).
- Show the top N *and* let the user see the full list. The list informs the pick; it never
  constrains it.
- On a double-dog week (§2.2) the list is unchanged — take the top `dogAllowance` rows. The picks
  are independent, so there is no joint-probability tiebreak here, unlike the starter trio in
  §7.4.

**Why not a user-settable spread threshold.** Across the realistic range at σ = 13.5, EV peaks
near +9.5 (2.41) and everything from +6.5 to +13.5 sits inside 2.20–2.41 — a 9-point-wide
plateau varying under 10%. Any threshold set inside that band would cut a flat surface
arbitrarily, discarding candidates statistically indistinguishable from the ones it keeps. The
only real edges are "below +5.5 the points are too small" and "above +14 the dog rarely wins",
and the EV ranking finds both without configuration.

The ceiling rule also hides an edge a threshold cannot express: because points round **up**, the
`.5` side of every rounding boundary pays the same as the whole number above it at a better win
rate — `+6.5` pays 7 at 0.3151 where `+7.0` pays 7 at 0.3020; `+9.5` pays 10 at 0.2408 where
`+10.0` pays 10 at 0.2294. A ranking surfaces that automatically.

**Acceptance case** — `Fixtures/expected/dog-recommendation-2026-week3.json`, all 16 week-3 dogs:

| Rank | Dog | Line | Spread | Pts | p(win) | EV |
|---|---|---|---|---|---|---|
| 1 | WSH | `SEA -8.5` | +8.5 | 9 | 0.2645 | **2.3805** |
| 2 | ARI | `SF -7.5` | +7.5 | 8 | 0.2893 | 2.3144 |
| 3 | MIA | `KC -10` | +10 | 10 | 0.2294 | 2.2940 |
| 4 | LAC | `BUF -7` | +7 | 7 | 0.3020 | 2.1140 |
| 5 | NYJ | `DET -7` | +7 | 7 | 0.3020 | 2.1140 |
| 6 | ATL | `GB -4.5` | +4.5 | 5 | 0.3694 | 1.8470 |
| 7–9 | CHI, LV, PIT | `-3.5` | +3.5 | 4 | 0.3977 | 1.5908 |
| 10–11 | CLE, TEN | `-2.5` | +2.5 | 3 | 0.4265 | 1.2795 |
| 12–13 | DAL, NE | `-3` | +3 | 3 | 0.4121 | 1.2363 |
| 14–16 | IND, LAR, MIN | `-1.5` | +1.5 | 2 | 0.4558 | 0.9116 |

**Recommended dog: WSH.** Three things in that table are worth asserting as behaviour, not just
as numbers:

- **MIA +10 is the biggest spread on the board and ranks third.** More points is not more EV.
- **CLE/TEN +2.5 outrank DAL/NE +3** on identical points — the `.5` edge, in the fixture.
- Ranks 1–5 span 2.11–2.38. A threshold anywhere in that band is noise.

### 7.6 The method doc's worked example does not reproduce — use this one instead

`docs/Swan League — Weekly Starter Method.md` claims that in week 3 the tiebreak "flipped the
49ers over the Saints for the third starting slot — 48.9% vs 38.6% perfect-week odds." Those
numbers came from a moneyline snapshot and **cannot be reproduced from the spread at any
plausible σ**. Against the real closing week-3 2026 lines (`SF -7.5`, `NO -3.5`) the Saints lead
the 49ers by 0.2769 EV — far outside the 0.05 band — so no tiebreak fires between them at all.
The doc's *direction* survives (the 49ers do carry the higher win probability), but its figures
and its pairing do not.

**Phase 3's acceptance case is replaced by the following**, computed from committed fixtures:

| Rank | Team | Pts | Line | p(win) | EV |
|---|---|---|---|---|---|
| 1 | Lions | 4 | `DET -7` | 0.6980 | **2.7918** |
| 2 | Saints | 4 | `NO -3.5` | 0.6023 | **2.4091** |
| 3 | Chiefs | 3 | `KC -10` | 0.7706 | **2.3117** |
| 4 | Giants | 4 | `NYG -2.5` | 0.5735 | **2.2938** |
| 5 | 49ers | 3 | `SF -7.5` | 0.7107 | 2.1322 |
| 6 | Bengals | 3 | `CIN -3.5` | 0.6023 | 1.8068 |
| 7 | Vikings | 3 | `TB -1.5` | 0.4558 | 1.3673 |
| 8 | Ravens | 2 | `BAL -3` | 0.5879 | 1.1759 |
| 9 | Rams | 2 | `DEN -1.5` | 0.4558 | 0.9115 |

Chiefs and Giants sit **0.0179 EV apart** — inside the 0.05 band — so the third slot goes to the
perfect-week tiebreak, with Lions and Saints locked:

- Chiefs: 0.6980 × 0.6023 × 0.7706 = **0.3240**
- Giants: 0.6980 × 0.6023 × 0.5735 = 0.2411

**Recommended starters: Lions, Saints, Chiefs.**

Assert the **outcome**, not the raw EV order. The trio is stable for σ ∈ [13.0, 14.5]; at
σ = 14.5 the Giants edge ahead of the Chiefs on raw EV and the tiebreak still selects the Chiefs.
That is precisely the behaviour worth locking down.

---

## 8. Going Perf and 0fer — settled

Both are **season records held by a single coach**, each paying half the fine pool
(`Rules!C31:C32`). **They are max/min over qualifying weeks, not counts.**
`docs/Personal Tracker + UI Redesign Plan.md` item 6 ("awarded for most occurrences over the
season") is **wrong** and is superseded here.

### 8.1 Definitions

- **Going Perf week:** all 4 picks win — the 3 started teams *and* the dog (`Rules!C45`). On a
  double-dog week, every dog pick must win.
- **0fer week:** all 3 started teams lose *and* the dog loses (`Rules!C47`). Ties count as
  losses, so a tie still qualifies a week as an 0fer.

Note the asymmetry with the Perfect Week bonus: **Perfect Week (+3) is 3 starters; Going Perf is
4 picks.** A week can be a Perfect Week without being a Going Perf. Fixture case
`samt-week-1-perfect-but-not-going-perf` exists specifically to hold that line.

### 8.2 The one metric both records use

Define, for any week:

```
weekPotential(week) = Σ startedTeam.PointValue          (all 3, regardless of result)
                    + Σ dogPick.dogPoints               (all dogs, regardless of result)
                    + 3                                 (the Perfect Week bonus)
```

That is "the point total that was possible" that week. Then:

- **Going Perf record** = `max(weekPotential)` over the coach's Going Perf weeks.
- **0fer record** = `min(weekPotential)` over the coach's 0fer weeks.

This resolves the open sub-question about what "lowest point total (that was possible)" sums: it
sums the **forfeited** points — the started teams' full point values plus every dog's full spread
value plus the +3 — because in an 0fer week nothing was actually scored.

Using one formula for both is not a convenience. In a Going Perf week every pick won, so the
points actually scored **equal** `weekPotential`. The workbook's "highest point total during a
perfect week" and "lowest point total that was possible during an 0fer week" are the same
quantity, maximized in one case and minimized in the other.

### 8.3 Verified against the workbook

`Scoreboard!G3` records the season's Going Perf record as **27.0**, held by Sam T. Sam T's week 2
(`Scoreboard!B166:W176`) was Chiefs 3 + 49ers 3 + Rams 2 = 8 starter points, Perfect Week +3, dog
16 → **8 + 3 + 16 = 27**. The formula reproduces the workbook's own recorded record exactly,
which confirms both that the +3 bonus is included and that dog points are included.

The `weekPotential` +3 term is constant across all weeks, so it never changes which week wins
either record. It is included for semantic correctness, and because the Going Perf verification
above only works with it.

The 0fer record cell (`Scoreboard!I3`) is empty — no coach had an 0fer week through week 2.
Treat "no qualifying week" as *no record holder*, not as a record of 0.

---

## 9. Deadlines and fines

- Starters: **Sunday 1:00 PM ET**, via the commissioner's Google form (`Rules!C2`).
- Each dog pick: before **that specific game** kicks off, so a Monday-night dog can be submitted
  Monday evening (`Rules!C7`).
- Missed submission: **$5 fine** per infraction, into the fine pool (`Rules!C15`).
- No reminders — the commissioner does not look at picks until after the deadline, so a missed
  pick is invisible until it is too late (`Rules!C37`).

The app logs what the coach submitted; it does not submit anything anywhere.

---

## 10. Corrections to the existing docs

Two factual errors found while building the fixtures, both in
`docs/Swan League — Rules.md`. Both would have produced wrong behaviour.

**a) The Ravens' 2026 bye week is 13, not 7.** The doc's bye table lists Week 7. Derived from the
real 2026 schedule (all 18 weeks, 32 teams, exactly one bye each), Baltimore's bye is **Week 13**.
Week 7 is when Baltimore plays Cincinnati — an intra-roster collision, which the doc appears to
have mistaken for a bye. Taking the doc at face value would have made the Ravens unstartable in
week 7 and startable on their actual bye in week 13.

**b) There are 18 intra-roster collision weeks, not 4.** The doc lists only "Lions vs. Vikings
(×2) and 49ers vs. Rams (×2) — four weeks", i.e. only the same-division pairs. The 9-team roster
spans six divisions and actually collides 18 times across the season (weeks 1, 1, 2, 2, 5, 6, 7,
8, 11, 12, 13, 13, 14, 14, 15, 16, 16, 17). The full list is in
`Fixtures/roster-2026.json → intraRosterCollisions`. This matters for §7.4's "never bench both"
rule, which now applies four times as often as the doc implied.

The correct bye weeks and collisions are in the fixtures. Read them from there, not from prose.

---

## 11. Resolved: the old open questions

`docs/Swan League — Rules.md` §"Open / unresolved rules" is fully closed:

| Question | Answer | Where |
|---|---|---|
| Do dog points scale by point value, or pay flat? | Neither — they pay the **spread, rounded up**. Roster point value is irrelevant, and the dog need not be rostered. | §2.1 |
| Do dog points count toward the main pool, or only the dog pool? | **Both.** | §2.1, §5 |
| Can a team be both a weekly start and the dog pick? | **Yes**, and both score. | §2 |
| What do the colour tiers mean in the regular-season tiebreaker? | Point values: red 1, yellow 2, blue 3, green 4. The tiebreaker walks them **green → blue → yellow → red**. | §1, §5.1 |
| What compensation do #1 seeds get for the Wild Card bye? | **+10 flat** to any coach rostering the conference winner. | §6 |
| Going Perf / 0fer by most occurrences, or first occurrence? | **Neither** — max/min point total over qualifying weeks. | §8 |
| Can the user set a spread threshold for dog picks? | **No.** No threshold, no filter of any kind. The app ranks every dog on the slate by EV; the pick stays wide open. Board decision, 2026-09-28. | §2.1, §7.5 |

---

## 12. Fixtures

Committed under `src/AgainstTheSpread.Tests/Fixtures/`. See that directory's `README.md` for
provenance and re-recording instructions.

| File | Purpose |
|---|---|
| `teams-2026.json` | 32 teams: 2025 finish, point value, tier + hex, 2026 bye week, workbook name aliases |
| `roster-2026.json` | the coach's 9 teams, point values, bye weeks, all 18 collision weeks |
| `odds-espn-2026-week3.json` | recorded odds payloads, all 16 week-3 games |
| `scores-espn-2026-week1.json` | recorded scores payload, week 1, all 16 final |
| `scores-espn-2026-week3.json` | recorded scores payload, week 3, 15 final + 1 scheduled |
| `expected/lines-2026-week3.json` | expected normalized `ILinesProvider` output, incl. dog points |
| `expected/scoring-2026-workbook-cases.json` | golden scoring cases taken from the workbook |
| `expected/recommendation-2026-week3.json` | golden starter recommendation from the §7.4 function |
| `expected/dog-recommendation-2026-week3.json` | golden ranked dog list from the §7.5 function, all 16 week-3 dogs |

`teams-2026.json → workbookAliases` exists because the workbook spells teams inconsistently —
`Cincinatti Bengals`, `Detriot Lions`, `Las Angeles Rams`, `LA Rams`, `Los Angeles Rams`,
`Tampa Brady`, `Team from Washington`, `New England Cheaters`, `Carolina Bakeshow`, plus
pervasive trailing whitespace. 38 distinct spellings for 32 teams. Any parser of the
commissioner's sheet must trim, case-fold and match against the alias list.
