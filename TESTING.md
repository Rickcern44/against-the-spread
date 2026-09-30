# Testing Guide

Comprehensive testing strategy and instructions for the Against The Spread application.

## Overview

This project uses two types of automated tests:

1. **Unit Tests** - Test individual components in isolation (this document)
2. **End-to-End Tests** - Test complete user flows with Playwright (see `tests/README.md`)

**CRITICAL: All tests must pass before any task is complete:**
```bash
# Run unit tests (156+ tests)
dotnet test

# Run E2E tests (2+ tests)
./start-e2e.sh
cd tests && npm test
./stop-e2e.sh
```

## Unit Test Structure

```
src/AgainstTheSpread.Tests/
├── Models/              # Unit tests for domain models
│   ├── GameTests.cs
│   ├── UserPicksTests.cs
│   └── WeeklyLinesTests.cs
├── Services/            # Unit tests for services
│   ├── ExcelServiceTests.cs
│   └── StorageServiceTests.cs
└── Functions/           # Unit tests for API functions
    └── FunctionsTests.cs
```

## Running Tests

### All Tests

```bash
dotnet test
```

### Specific Test Project

```bash
dotnet test src/AgainstTheSpread.Tests/AgainstTheSpread.Tests.csproj
```

### With Coverage

```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Verbose Output

```bash
dotnet test --verbosity detailed
```

## Test Coverage

Current test coverage:

- **Models**: 100% - All validation logic tested
- **Services**: 90% - Core logic tested, integration tests deferred
- **Functions**: 70% - Constructor and DI tested, HTTP integration deferred

### Coverage by Phase:

**Phase 1 - Models (57 tests)**
- ✅ UserPicks validation (15 tests)
- ✅ WeeklyLines validation (9 tests)
- ✅ Game display properties (11 tests)

**Phase 2 - Excel Service (10 tests)**
- ✅ Parse weekly lines from Excel
- ✅ Generate picks Excel in exact format
- ✅ Handle invalid/empty files
- ✅ Validate Excel structure

**Phase 3 - Storage Service (3 tests)**
- ✅ Service construction
- ⚠️ Blob operations (placeholder - needs Azurite)

**Phase 4 - Functions API (3 tests)**
- ✅ Function construction and DI
- ⚠️ HTTP request/response (deferred to integration tests)

**Total: 73 tests, all passing**

## Manual Testing Checklist

### End-to-End User Flow

#### Prerequisites
- [ ] Azure infrastructure deployed via Terraform
- [ ] Function App deployed and running
- [ ] Static Web App deployed
- [ ] At least one week's lines uploaded

#### Test Steps

1. **Home Page**
   - [ ] Navigate to Static Web App URL
   - [ ] Verify page loads with "Against The Spread" title
   - [ ] Click "Make Your Picks" button
   - [ ] Redirects to /picks page

2. **Week Selection**
   - [ ] Enter your name (e.g., "Test User")
   - [ ] Select current year
   - [ ] Verify week dropdown populates with available weeks
   - [ ] Select Week 1
   - [ ] Click "Continue to Picks"
   - [ ] Verify games load

3. **Game Picking**
   - [ ] Verify all games are displayed with dates
   - [ ] Verify each game shows Favorite (-X.X) and Underdog
   - [ ] Click first game's favorite button
   - [ ] Verify button turns green with checkmark
   - [ ] Verify counter shows "Selected: 1 / 6 picks"
   - [ ] Pick 5 more games (total 6)
   - [ ] Verify counter shows "Selected: 6 / 6 picks ✓ Ready to download!"
   - [ ] Try clicking a 7th game
   - [ ] Verify button is disabled (can't pick more than 6)
   - [ ] Click a selected game again
   - [ ] Verify it deselects (can change picks)

4. **Excel Download**
   - [ ] Verify download button appears at bottom when 6 picks selected
   - [ ] Click "Download Your Picks (Excel)" button
   - [ ] Verify button shows "Generating Excel..." with spinner
   - [ ] Verify file downloads: `Your_Name_Week_1_Picks.xlsx`
   - [ ] Open Excel file
   - [ ] Verify format matches "Weekly Picks Example.csv":
     * Row 1: Empty
     * Row 2: Empty
     * Row 3: Headers (Name, Pick 1-6)
     * Row 4: Your name and 6 picks

5. **Back Navigation**
   - [ ] Click "← Back to Week Selection"
   - [ ] Verify returns to week selection screen
   - [ ] Verify name is preserved
   - [ ] Verify previous selections are cleared

### API Testing

#### Test GET /api/weeks

```bash
# Replace with your Function App URL
FUNC_URL="https://your-function-app.azurewebsites.net"

# Get available weeks for 2025
curl "$FUNC_URL/api/weeks?year=2025"

# Expected response:
# {
#   "year": 2025,
#   "weeks": [1, 2, 3]
# }
```

#### Test GET /api/lines/{week}

```bash
# Get lines for Week 1
curl "$FUNC_URL/api/lines/1?year=2025"

# Expected response: WeeklyLines JSON with games array
# {
#   "week": 1,
#   "year": 2025,
#   "games": [
#     {
#       "favorite": "Alabama",
#       "line": -9.5,
#       "vsAt": "vs",
#       "underdog": "Florida State",
#       ...
#     },
#     ...
#   ]
# }
```

#### Test POST /api/picks

```bash
# Submit picks and download Excel
curl -X POST "$FUNC_URL/api/picks" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Test User",
    "week": 1,
    "year": 2025,
    "picks": ["Alabama", "Ohio State", "Michigan", "Georgia", "Texas", "Clemson"]
  }' \
  --output picks.xlsx

# Verify file downloaded
file picks.xlsx
# Should show: Microsoft Excel 2007+

# Open and verify format
```

### Mobile Testing

Test on actual mobile devices or Chrome DevTools device mode:

#### iPhone (Safari)
- [ ] Test in portrait mode
- [ ] Test in landscape mode
- [ ] Verify buttons are easily tappable
- [ ] Verify download works on iOS

#### Android (Chrome)
- [ ] Test in portrait mode
- [ ] Test in landscape mode
- [ ] Verify buttons are easily tappable
- [ ] Verify download works on Android

### Browser Compatibility

- [ ] Chrome (latest)
- [ ] Firefox (latest)
- [ ] Safari (latest)
- [ ] Edge (latest)

## Excel Format Validation

Critical: Output Excel must EXACTLY match "Weekly Picks Example.csv" format.

### Validation Script

```bash
# Compare generated Excel with reference format
python3 << 'EOF'
import openpyxl

# Open generated file
wb = openpyxl.load_workbook('picks.xlsx')
ws = wb.active

# Validate structure
assert ws['A1'].value == '', "Row 1 should be empty"
assert ws['A2'].value == '', "Row 2 should be empty"
assert ws['A3'].value == "Name", "A3 should be 'Name'"
assert ws['B3'].value == "Pick 1", "B3 should be 'Pick 1'"
assert ws['G3'].value == "Pick 6", "G3 should be 'Pick 6'"

# Validate data row
assert ws['A4'].value is not None, "Row 4 should have name"
assert ws['B4'].value is not None, "Row 4 should have Pick 1"
assert ws['G4'].value is not None, "Row 4 should have Pick 6"

print("✅ Excel format validation passed!")
EOF
```

## Performance Testing

### Load Testing (Optional)

Use Apache Bench or Artillery to test API performance:

```bash
# Install Apache Bench
brew install apache-bench  # macOS

# Test GET /api/weeks
ab -n 1000 -c 10 "$FUNC_URL/api/weeks?year=2025"

# Test GET /api/lines/1
ab -n 1000 -c 10 "$FUNC_URL/api/lines/1?year=2025"
```

Expected results on Consumption plan:
- Average response time: < 500ms
- 95th percentile: < 1000ms
- No failures

## Security Testing

### API Security Checklist

- [ ] Anonymous access works for all endpoints
- [ ] CORS configured to allow web app origin
- [ ] Storage blobs are private (not publicly accessible)
- [ ] No sensitive data in API responses
- [ ] No SQL injection vectors (not using SQL)
- [ ] Proper error handling (no stack traces to user)

### Test CORS

```bash
# From browser console on web app
fetch('$FUNC_URL/api/weeks?year=2025')
  .then(r => r.json())
  .then(console.log)
  .catch(console.error)

# Should succeed without CORS error
```

### Test Storage Security

```bash
# Try to access blob directly (should fail)
curl "https://your-storage.blob.core.windows.net/gamefiles/lines/week-1-2025.xlsx"
# Expected: 404 or 403 (blob is private)
```

## Automated Integration Tests (Future Enhancement)

For Phase 8+, consider adding:

### Azurite Tests for Storage Service

```csharp
// Example using Azurite
[Fact]
public async Task StorageService_WithAzurite_UploadsAndRetrievesLines()
{
    // Start Azurite in Docker
    // Test actual blob operations
    // Verify JSON parsing
}
```

### HTTP Integration Tests for Functions

```csharp
// Example using Microsoft.AspNetCore.Mvc.Testing
[Fact]
public async Task GetWeeks_ReturnsValidResponse()
{
    var client = _factory.CreateClient();
    var response = await client.GetAsync("/api/weeks?year=2025");
    response.EnsureSuccessStatusCode();
}
```

## Known Issues / Future Improvements

### Current Limitations

1. **JSON Parsing in Upload Script**
   - Script creates placeholder JSON
   - Actual parsing happens on-demand by API
   - Future: Add .NET CLI tool to parse during upload

2. **No User Authentication**
   - MVP has no login/auth
   - Anyone can make picks
   - Future: Add Azure AD B2C or similar

3. **No Pick History**
   - Picks are downloaded, not stored
   - No way to retrieve past picks
   - Future: Store picks in Cosmos DB or Table Storage

4. **Limited Error Handling**
   - Basic error messages
   - Future: Better UX for network errors

### Testing Gaps

1. ~~E2E tests with Playwright or Selenium~~ **DONE - See `tests/README.md`**
2. Integration tests with real Azure services (Azurite setup needed)
3. Accessibility testing (WCAG compliance)
4. Internationalization testing

## Test Maintenance

### When to Update Tests

- **Code Changes**: Update tests before merging PR
- **New Features**: Add tests as part of feature implementation
- **Bug Fixes**: Add regression test before fixing bug
- **API Changes**: Update contract tests immediately

### Test Review Checklist

Before merging:
- [ ] All unit tests pass locally (`dotnet test`)
- [ ] All E2E tests pass locally (`cd tests && npm test`)
- [ ] CI passes on GitHub Actions
- [ ] Code coverage hasn't decreased
- [ ] New features have unit tests
- [ ] New user flows have E2E tests (if applicable)
- [ ] Tests follow AAA pattern (Arrange, Act, Assert)
- [ ] Tests are isolated (no shared state)
- [ ] Tests have descriptive names

## Continuous Improvement

### Metrics to Track

1. Test count trend
2. Code coverage percentage
3. Test execution time
4. Flaky test count (should be 0)
5. Bug escape rate (bugs found in prod vs tests)

### Goals

- Maintain 80%+ code coverage
- Keep test suite under 5 minutes
- Zero flaky tests
- Add integration tests in next iteration

## End-to-End Testing (Playwright)

For complete E2E testing documentation, see **[`tests/README.md`](tests/README.md)**.

### Quick Reference

```bash
# Start E2E environment
./start-e2e.sh

# Run E2E tests
cd tests && npm test

# Run with visible browser
npm run test:headed

# Debug tests
npm run test:debug

# Stop E2E environment
cd .. && ./stop-e2e.sh
```

### Key E2E Test Files

| File | Purpose |
|------|---------|
| `tests/README.md` | Complete E2E testing guide |
| `tests/specs/full-flow.spec.ts` | Main test scenarios |
| `tests/pages/admin-page.ts` | Admin page object model |
| `tests/pages/picks-page.ts` | Picks page object model |
| `tests/helpers/` | Test utilities and validators |
| `start-e2e.sh` | Start E2E environment |
| `stop-e2e.sh` | Stop E2E environment |

### When to Add E2E Tests

- New user-facing features
- Modified user flows or interactions
- Changed API endpoints used by the UI
- Updated authentication or authorization
