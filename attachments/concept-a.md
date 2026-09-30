# Weekly checklist: Swan League concept A

Design proposal · September 30, 2026 · **Review requested; implementation is not approved.**

## Basis and goal

Ricky uses this as a single-coach companion to record what he submitted in the commissioner's Google Form, see the result of his picks, and review his own totals. The initial view should answer: **Which selections are still missing, what have I recorded, and what scored?** The adjacent annotated screen board shows a 390px phone and a 1280px desktop structure. Its teams and scores are illustrative.

`docs/swan-league-spec.md` is authoritative for league rules. The workbook at `reference-docs/Weekly Picks Example.xlsx` has one six-pick college row and is legacy context. The commissioner workbook cited in the spec is absent from this checkout, so this proposal does not show opponent standings or a league rank.

## Screen behavior

1. **This week, before deadline.** Top: week selector, clear “Personal record” label, Sunday 1:00 PM ET starter deadline and the next dog's game kickoff. Progress is separated: “2 of 3 starters” and “0 of 1 dogs.” The checklist's missing item links to its picker. Saved picks are visible above suggestions. The starter picker contains only the user's nine roster teams, marks byes unavailable with a reason, and allows an explicit replacement when three are selected. The dog picker shows the full slate of eligible underdogs, independent of roster membership, with official line if available; a week-declared allowance of two shows “0 of 2 dogs.” A roster team may fill both roles.
2. **Review and save.** A persistent summary lists three distinct starters and the exact dog allowance. The primary action is “Save my record,” enabled only after required picks are complete. A successful save shows the time and “This records your picks here. Submit them separately in the commissioner's Google Form.” The design may offer a user-controlled “I submitted these picks” acknowledgement, but it must never call this official confirmation. A failed save preserves selections and gives a retry path. Do not auto-change picks from a recommendation.
3. **During and after games.** Each recorded pick shows pending/final state, final score, and its own earned points. Starter wins earn their roster point value; losses and ties earn zero. A +3 Perfect Week bonus appears only when all three starters win. Dog wins earn the ceiling of the commissioner's posted spread and enter Main and Dog totals; an API-line fallback is visibly “Unconfirmed line.” Dog covers and pushes are irrelevant to scoring. A team appearing in both roles gets two labeled score rows. Results can be provisional until all relevant games and line sources settle.
4. **My totals and history.** Show Main, Dog, Regular Season and Playoff as separately labeled totals; explain their overlapping contributions. History opens a week with recorded selections, line source, score breakdown, save time and any later correction. League standings appear only after an authorized opponent-data source is defined. No zero placeholder rank.

## Responsive layout and components

Phone: one column, actions and checklist first; bottom navigation has **This week**, **My totals**, **History**. The dog list is a separate section after starters so the full slate does not bury the three-roster decision. Review summary stays in the reading flow; a sticky save bar, if used, clears the safe area and navigation. Tablet: keep game rows full width, place progress and score summary side by side only when text fits. Desktop: two columns with pickers in the main area and a persistent review/score rail; tab order follows the visual reading order. At 320 CSS pixels and 200% zoom, content stacks without horizontal scrolling.

Components: week selector; deadline text; progress checklist; roster starter row with point-value chip; dog row with spread and line-source label; optional recommendation disclosure; review summary; save/status notice; score-breakdown row; four-pool total card; history row. The point tiers follow the spec's red/yellow/cyan/green mapping, but each chip includes “1 pt” through “4 pts” and uses contrast-safe text/surrounding surfaces rather than color alone.

## States and accessibility

- **Empty:** no slate, no picks, and no scored week each get separate messages and next actions. A missing official line shows a usable API estimate with “Unconfirmed for scoring,” not a blank dog.
- **Loading/error:** preserve structure and current draft; never show zero as a temporary score. Save failure states “Not saved,” explains retry, and keeps selections.
- **Success/locked:** show recorded time. Once Sunday 1:00 PM ET or a dog's kickoff passes, show the applicable closed state. Any later historical edit is labeled a personal correction, not a valid official submission.
- **Keyboard/screen reader:** semantic headings and grouped labeled choices; selected and disabled reasons in text; focus visible and clear of sticky UI; 44×44 CSS-pixel target goal; polite save announcement; no live-score focus jumps; table headers where totals become tabular. Check WCAG 2.2 AA contrast, reflow and focus criteria on an interactive prototype.

## Decisions needed before implementation

1. Is the proposed **“I submitted these picks”** acknowledgement useful, or should the record show only a save time and the Google Form reminder?
2. Where may opponent standings and the commissioner's current official lines be obtained, and may this personal tool use them? Until resolved, show personal totals only.
3. Should a historical personal correction be editable after the relevant official deadline? Its status must remain distinct from an on-time submission.

## Review criteria

In phone and desktop prototypes, a user can find missing picks, select three valid roster starters and the declared number of dogs, review and save without mistaking that action for a league submission, explain the points for each role, and find each pool's own total. A two-dog week, bye, shared starter/dog team, starter tie, missing official line, save failure, and locked pick have readable states. No opponent rank appears without verified data. Ask for explicit approval of this concept after reviewing the attached screens; revisions stay in design.
