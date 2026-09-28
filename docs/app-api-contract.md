# App API contract

This is the Phase 1.5 wire-contract lock. All responses use `ApiResponse<T>`: successful calls set
`data`; expected empty/error states set `problem` with `code`, `message`, and `isRetryable`.

## Identifier convention

Every `Team.Id`, team ID in a `WeeklyGame`, `WeeklyPick`, recommendation, and fixture is the
canonical **ESPN numeric team ID**, serialized as a string (for example, Philadelphia = `"21"`).
This matches `tests/Fixtures/teams-2026.json` provider identity. UI display names, abbreviations,
and logo/color aliases are presentation mappings only; they must never be used as wire IDs.

## Routes

| Method | Route | Body | `data` payload |
| --- | --- | --- | --- |
| GET | `/api/weeks/{week}/games` | — | `WeekGamesResponse` (`WeeklyGame`, its participating `Team` records, API, official, derived scoring lines and optional result) |
| GET | `/api/weeks/{week}/picks` | — | `WeekPicksResponse` |
| PUT | `/api/weeks/{week}/picks` | `SaveWeekPicksRequest` | `WeekPicksResponse` |
| GET | `/api/weeks/{week}/recommendation` | — | `WeekRecommendationResponse`: independently ranked starter and dog lists |
| GET | `/api/standings` | — | `StandingsResponse`: aggregate `SeasonScore` plus weekly history |
| GET | `/api/weeks/{week}/data` | — | `WeekDataStatusResponse` |
| POST | `/api/weeks/{week}/data` | `PostWeekDataRequest` | `WeekDataStatusResponse` |

`PostWeekDataRequest.Pull` manually retriggers lines or results. `PostWeekDataRequest.OfficialLineOverride`
writes the commissioner's positive favorite line for one game. The two shapes are mutually exclusive.

Expected problem codes: `WeekNotPulled`, `ProviderUnavailable`, `NoPicksLogged`, and
`SeasonNotStarted`. Frontend code uses `IAppApiClient` exclusively; it does not call `HttpClient`.
