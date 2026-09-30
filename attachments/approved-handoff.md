# Approved design handoff: Weekly checklist (A)

Approved by Ricky in the issue comment on September 30, 2026. This handoff supplements `concept-a.md` and `concept-a-screens.svg`. The screens are illustrative; `docs/swan-league-spec.md` controls league rules and `docs/app-api-contract.md` controls the app data contract.

## User and navigation

The sole user is a coach recording their own Swan League picks and reviewing personal scores. Primary destinations are **This week**, **My totals**, and **History**. Keep roster management, data source details, and rules in secondary navigation. The app is a personal record; the commissioner Google Form remains the submission channel. An opponent spreadsheet supplied by the user can populate standings after its columns and week coverage are verified; until then, do not show a league position.

## Screen behavior

### This week

1. Start with season/week selector, **Personal record** label, and the next actionable deadline in Eastern time. State both deadline rules: starters close Sunday at 1:00 PM ET; each dog closes at its own kickoff. Show separate progress such as **2 of 3 starters** and **0 of 2 dogs**. A missing-item link moves focus to the relevant picker.
2. Display saved selections first, then the available choices. Starter choices come from the user's nine roster teams only. Show team, opponent, kickoff, season point value, selection state, and bye or lock reason. Allow exactly three distinct starters. At three selected, choosing another must expose an explicit replacement action so a saved choice is never silently dropped.
3. The dog section is independent of starters. Show the complete eligible underdog slate, team, opponent, signed spread, line source, kickoff, and selection state. Label the user's ESPN line and its week/snapshot when available. A dog may also be a starter. The per-week dog allowance defaults to one but is editable when the commissioner declares a different number; display the declared allowance beside progress. Do not hardcode special weeks or impose a spread or roster filter. Keep recommendations in a labeled, optional disclosure; selecting one is always deliberate.
4. Review lists the exact three starter names and the declared number of dog names. **Save my record** is enabled when required choices are complete and currently editable. On success, show the saved time and: **Saved here as your personal record. Submit picks separately in the commissioner's Google Form.** Never label this as an accepted league submission. On failure, keep the draft, show **Not saved**, and offer Retry. When a deadline passes, preserve the recorded choices for viewing and explain which selection is locked.

### My totals

Show four distinct cards: **Main**, **Dog**, **Regular Season**, and **Playoff**. Explain that pools overlap; they are not four parts of one sum. Distinguish settled, pending, and unconfirmed points. A compact breakdown links to the week that produced points. Once the user supplies an opponent spreadsheet and its columns are verified, show the imported standings with an **As of week/date** label and identify ties; never imply they update live. Until then, do not render an invented rank, a zero placeholder rank, or a standings table.

### History and week detail

List 18 regular-season weeks and one combined **CC/P** playoff slot, with saved timestamp and settled/provisional status. Opening a week shows starters, dogs, result, points by role, Perfect Week bonus, line source, and correction metadata when present. Provide **Correct my record** after a deadline; show the prior value, require an explicit Save correction, timestamp the change, and recalculate personal totals. Label it **Personal correction — does not change the commissioner's submission**. A team picked in both roles gets two labeled score rows. Starter win pays that team's 1–4 point value; loss or tie pays zero. All three starters winning adds +3, independent of dogs. A dog must win outright and scores `ceil(positive official spread)`; a loss or tie pays zero. For now, use the saved ESPN line snapshot as the assumed commissioner scoring line and show its source and capture time alongside dog points. Label ESPN-based dog points **Assumed line** until the commissioner line is verified; a different confirmed official line takes precedence under the league spec. Allow the line in the personal record to be corrected later and recalculate affected dog points and totals. Dog points contribute to Main and Dog, while starter and Perfect Week points contribute to Main and Regular Season. Playoff detail follows the spec's separate multipliers and bonuses.

## Responsive layout

- **Phone, 320–599 CSS px:** one column. Progress and saved selections precede pickers; starters precede full dog slate. Bottom navigation has three labeled destinations. Review is in reading order. If Save is sticky, reserve room above navigation and the safe area; it must not cover content or focused controls.
- **Tablet, 600–1023 CSS px:** retain full-width game rows. Put progress and personal score summary side by side only when each has readable space; otherwise stack.
- **Desktop, 1024 CSS px and up:** main column contains the checklist and pickers; a persistent right rail contains review, deadlines, and score summary. DOM and keyboard order still follow the mobile reading sequence. At 320 CSS px and 200% zoom, there is no horizontal scrolling or clipped action.

## Components, assets, and tokens

Build from week selector, deadline/progress checklist, starter choice row, dog choice row, optional recommendation disclosure, review summary, save notice, score breakdown, four pool cards, and history row. Team display names and optional logos are presentation only; use canonical ESPN string IDs for data. The existing application font and surface tokens in `src/AgainstTheSpread.Web/wwwroot/css/app.css` are the visual baseline: IBM Plex Sans for body, Teko for short display headings, IBM Plex Mono for numeric detail; `--ats-bg`, `--ats-surface`, `--ats-surface-raised`, `--ats-text`, `--ats-line`, `--ats-gold`, and `--ats-teal`. Preserve the league's exact tier hues as small labeled swatches: 1 pt `#FF0000`, 2 pt `#FFFF00`, 3 pt `#00FFFF`, 4 pt `#00FF00`. Place readable **1 pt**–**4 pts** text on a contrast-safe surrounding surface; hue alone never communicates value. Use current team color mapping only as decorative support, not as status.

## Interaction and accessibility states

- **Empty:** distinguish no week data, no saved picks, no opponent spreadsheet, and no scored history; give the appropriate next action. **Loading:** keep stable layout and avoid temporary zero scores. **Error:** explain whether data retrieval, spreadsheet import, or saving failed; retain any draft or prior imported standings and provide a retry. **Success:** timestamp the saved personal record or correction and show the imported standings' week/date. **Disabled/locked:** give the bye or exact deadline reason beside the selection control while keeping **Correct my record** available for history. **Partial result:** identify pending games and assumed lines awaiting verification. **Correction:** label personal historical changes distinctly from on-time official picks.
- Use semantic headings; group choices with a clear label and selected state; expose disabled reasons in text. All pickers, week selection, disclosures, and Save must work by keyboard. Use visible focus that is not obscured by a sticky bar, a 44×44 CSS px target goal, descriptive button names, and polite save-status announcements. Do not move focus for live score updates. Check WCAG 2.2 AA text/non-text contrast, reflow, and focus visibility in an interactive build.

## Testable acceptance criteria

1. On phone and desktop, a user can find missing picks, select three distinct non-bye roster starters, set a declared two-dog allowance, pick two eligible dogs from the full slate, review the exact selections, and save a personal record.
2. Replacing a fourth starter never silently drops a prior choice. A roster team can be both starter and dog; the review and score breakdown show both roles.
3. The UI displays the starter and per-dog deadlines in ET, prevents a locked selection from appearing actionable as an on-time pick, and permits an explicitly labeled personal correction with timestamp and recalculated totals.
4. A save failure preserves selections; a successful save provides time and the separate Google Form reminder. No UI text implies commissioner acceptance.
5. A starter tie earns zero and prevents the +3 bonus. Dog scoring uses an outright win and the ceiling of the assumed ESPN spread until the commissioner scoring line is verified; the assumption, source, and capture time are visible. A later line correction recalculates dog points and totals. Pending games do not appear as losses.
6. Four pool totals remain distinct and explain overlapping contributions. Opponent standings appear only after spreadsheet import succeeds, with a visible week/date and tie treatment; missing or malformed data never becomes a fabricated rank.
7. Keyboard and screen-reader users can complete the core flow, perceive selection/disabled/status changes without color, and reach every action at 320 CSS px and 200% zoom.

## Open decisions and implementation boundary

- **Submission acknowledgement:** this means an optional user-controlled “I submitted these picks in the commissioner's Google Form” checkbox, solely as a personal reminder. Omit unless the user requests it; a save time alone never claims submission.
- **Other coaches:** the user will upload a spreadsheet for opponent standings. Its schema, coverage, and update cadence remain unknown until received.
- **Line authority — working assumption:** the user uses ESPN lines and has asked us to assume that the same ESPN snapshot is the commissioner's posted scoring line for now. Keep the snapshot source/time and **Assumed line** label visible until verified. If the commissioner used a different line, the personal record can be corrected and affected dog points and totals recalculated; the confirmed commissioner line governs under `docs/swan-league-spec.md`.
- **Post-deadline personal corrections:** allowed in the personal record with a visible correction timestamp; they never imply a changed official submission.
- **Postponed/canceled games:** league treatment is not defined in the available spec. Surface a pending/unresolved result rather than awarding points automatically.

Approval covers this UI direction. Any later rule or data-source decision should update the affected labels and states before a frontend release.
