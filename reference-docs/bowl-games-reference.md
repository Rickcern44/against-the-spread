# Bowl Games Reference Documentation

This document describes the bowl games format and rules based on the game administrator's specifications.

## Bowl Game Rules

### Overview

Bowl games work differently from regular season picks:

1. **36 Bowl Games Total** - All games must be picked
2. **Three Data Points Per Game**:
   - Winner against the spread
   - Confidence points (1-36)
   - Outright winner (regardless of spread)

### Payouts

There are two separate payouts for bowl games:

| Payout | Prize | Description |
|--------|-------|-------------|
| Confidence Points | $1,000 | Person with most confidence points earned |
| Outright Wins | $1,000 | Person with most games picked correctly outright |

*If there is a tie, the pot is split among the winners.*

### Deadline

**PICKS MUST BE SUBMITTED BY FRIDAY** - Late picks will NOT be accepted.

### Team Name Matching

**CRITICAL**: Team names must be spelled EXACTLY as they appear on the Lines sheet. If the name is not spelled correctly, it will count as a LOSS.

---

## Bowl Template Format

### User Input Requirements

On the Bowl Template spreadsheet, users must input 4 pieces of data:

1. **Your Name** - In the yellow cell
2. **Winner Against the Spread** - Team that covers the spread
3. **Confidence Points** (1-36):
   - Each game gets a unique value
   - 36 = MOST CONFIDENT game
   - 1 = LEAST CONFIDENT game
   - Sum must equal 703 (1+2+3+...+36)
4. **Outright Winner** - Team that wins regardless of spread

### Validation

- Cell H43 shows the sum of confidence points
- If sum = 703: Cell turns **GREEN** ✅
- If sum ≠ 703: Cell turns **RED** ❌

### Important Rules

- **DO NOT CHANGE THE ORDER OF THE GAMES ON THE TEMPLATE**
- All 36 games must have picks
- No duplicate confidence points allowed
- Team names must match exactly

---

## Excel File Formats

### Bowl Lines File (Admin Upload)

Expected columns:
- Bowl Name (e.g., "Rose Bowl", "Sugar Bowl")
- Date/Time
- Favorite (team name)
- Line (point spread, e.g., -7.5)
- Underdog (team name)

Example:
| Bowl Name | Date | Favorite | Line | Underdog |
|-----------|------|----------|------|----------|
| Rose Bowl | 1/1/2025 5:00 PM | Oregon | -3.5 | Ohio State |
| Sugar Bowl | 1/1/2025 8:45 PM | Georgia | -6.5 | Baylor |

### Bowl Template File (User Output)

Expected format:
```
Row 1: [Name label] [User's name - YELLOW CELL]
Row 2: [Empty]
Row 3: [Headers: Game #, Winner vs Spread, Confidence, Outright Winner]
Row 4-39: [Picks for games 1-36]
Row 40+: [Validation area]

Cell H43: Sum formula (=SUM of confidence points)
         Green if =703, Red otherwise
```

---

## API Endpoints

### GET /api/bowl-lines?year={year}

Returns all bowl games with betting lines.

**Response:**
```json
{
  "year": 2024,
  "games": [
    {
      "gameNumber": 1,
      "bowlName": "Rose Bowl",
      "favorite": "Oregon",
      "line": -3.5,
      "underdog": "Ohio State",
      "gameDate": "2025-01-01T17:00:00Z"
    }
  ],
  "uploadedAt": "2024-12-15T12:00:00Z"
}
```

### POST /api/bowl-picks

Submits user picks and returns Excel file.

**Request:**
```json
{
  "name": "John Smith",
  "year": 2024,
  "picks": [
    {
      "gameNumber": 1,
      "spreadPick": "Oregon",
      "confidencePoints": 35,
      "outrightWinner": "Oregon"
    }
  ]
}
```

**Response:** Excel file download

---

## Confidence Points Math

The sum of 1 + 2 + 3 + ... + N can be calculated using the formula:

```
Sum = N(N+1)/2
```

### Analysis of Requirements

The original requirements state:
- "There are 36 games so each game will have a value of 1 - 36"
- "If you get the confidence points correct, you will have 703 in cell H43"

However, mathematically:
- 1+2+3+...+36 = 36 × 37 / 2 = **666**
- 1+2+3+...+37 = 37 × 38 / 2 = **703**

### Resolution

There are a few possible explanations:
1. There may actually be 37 bowl games in the template
2. The confidence points might start at 0 (0+1+2+...+36 = 666, still doesn't equal 703)
3. There might be bonus points added
4. The requirements may have a typo

**Implementation Approach**: The implementation should be **dynamic** and calculate the expected sum based on the actual number of games in the uploaded bowl lines file:

```csharp
// Calculate expected sum dynamically
int expectedSum = totalGames * (totalGames + 1) / 2;
```

This way, the system will work correctly regardless of whether there are 36, 37, or any other number of bowl games.

**Note for Implementation**: When the actual Bowl Template and Bowl Lines files are available, verify:
1. The exact number of games
2. The range of confidence points
3. The expected sum shown in the validation cell

---

## Differences from Regular Season

| Aspect | Regular Season | Bowl Games |
|--------|---------------|------------|
| Frequency | Weekly (14 weeks) | Once per season |
| Games per pick | 6 | All games (typically 36-43) |
| Pick type | Team vs spread only | Spread + Confidence + Outright |
| Confidence | None | 1-N (unique, sum = N(N+1)/2) |
| Validation | Must select exactly 6 | Sum of confidence points |
| Payouts | 1 (all correct wins) | 2 (confidence + outright) |
| Deadline | Thursday kickoff | Friday |

---

## Storage

Bowl files should be stored separately from regular season files:

```
gamefiles/
├── lines/
│   ├── week-1.xlsx
│   ├── week-2.xlsx
│   └── ...
└── bowl-lines/
    ├── 2024.xlsx
    ├── 2025.xlsx
    └── ...
```

This keeps bowl games completely separate from regular season data.
