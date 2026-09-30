# E2E Test Debugging Guide

## Overview
This document helps debug E2E test failures for the bowl games implementation.

## Test Setup

### Storage Isolation
Bowl games and regular season use **different blob storage paths**:
- Regular season: `lines/week-{week}-{year}.json`
- Bowl games: `bowl-lines/bowls-{year}.json`

Both can safely use year 2025 without conflicts.

### Test Configuration
- **Sequential execution**: Tests run with `workers: 1` and `fullyParallel: false`
- **Browser**: Chromium only
- **Base URL**: `http://localhost:4280` (SWA CLI with mock auth)

### Services Required
All four services must be running (started via `./start-e2e.sh`):
1. **Azurite** (port 10000) - Storage emulator
2. **Azure Functions** (port 7071) - Backend API
3. **Blazor Web App** (port 5158) - Frontend
4. **SWA CLI** (port 4280) - Auth proxy (entry point)

## Admin Page Changes

### What Was Added
Bowl upload section added to Admin.razor with new IDs:
- `#bowlYearInput` (bowl year)
- `#bowlFileInput` (bowl file upload)
- `#uploadBowlButton` (upload button)

### What Was NOT Changed
Original regular season upload section intact with IDs:
- `#weekInput` (week number)
- `#yearInput` (year)
- `#fileInput` (file upload)
- Upload button still works

## Common Failure Scenarios

### 1. Service Startup Failure
**Symptom**: Tests fail immediately or timeout
**Check**:
```bash
# Verify all ports are listening
lsof -i :10000  # Azurite
lsof -i :7071   # Functions
lsof -i :5158   # Web
lsof -i :4280   # SWA CLI

# Check service logs
cat /tmp/func-e2e.log
cat /tmp/web-e2e.log
cat /tmp/swa-e2e.log
```

### 2. Storage Conflicts
**Symptom**: Tests fail when accessing blob storage
**Check**:
```bash
# Verify blob paths are different
# Bowl: bowl-lines/bowls-2025.json
# Regular: lines/week-11-2025.json
```

### 3. Admin Upload Selector Issues
**Symptom**: Regular season tests can't find upload elements
**Verify**: 
- `#weekInput`, `#yearInput`, `#fileInput` still exist in Admin.razor
- Bowl section uses different IDs: `#bowlYearInput`, `#bowlFileInput`

### 4. Race Conditions
**Symptom**: Intermittent failures
**Check**:
- Tests run sequentially (workers=1)
- Proper wait conditions in page objects
- `waitForLoadState('networkidle')` used appropriately

### 5. Bowl API Endpoints
**Symptom**: Bowl tests fail to load data
**Verify endpoints are accessible**:
```bash
curl http://localhost:7071/api/bowl-lines?year=2025
curl http://localhost:7071/api/bowl-lines/exists?year=2025
```

## Test Files

### Test Specs
1. `tests/specs/full-flow.spec.ts` - Regular season (Week 11, Week 12)
2. `tests/specs/bowl-flow.spec.ts` - Bowl games (3 tests)

### Test Execution Order
Tests run alphabetically:
1. bowl-flow.spec.ts (runs first)
2. full-flow.spec.ts (runs second)

If bowl tests leave state that breaks regular season tests, this would be the issue.

## Debugging Steps

### Step 1: Run Locally
```bash
# Start services
./start-e2e.sh

# In another terminal
cd tests
npm test

# Check for failures
npm run test:report
```

### Step 2: Run Individual Test Files
```bash
# Test bowl games only
npx playwright test bowl-flow.spec.ts

# Test regular season only
npx playwright test full-flow.spec.ts
```

### Step 3: Check Logs
```bash
# Service logs
tail -f /tmp/func-e2e.log
tail -f /tmp/web-e2e.log
tail -f /tmp/swa-e2e.log

# Test output
cat tests/playwright-report/index.html
```

### Step 4: Verify Unit Tests Pass
```bash
dotnet test --verbosity minimal
# Should show: Passed! - 214 tests
```

## CI-Specific Issues

### GitHub Actions Environment
- Uses Ubuntu latest
- Installs dependencies fresh each run
- May have different timing than local

### Check CI Logs
Look for:
1. Service startup errors
2. Port conflicts
3. Blob storage initialization errors
4. Timeout errors
5. Selector not found errors

## Expected Test Results

### Unit Tests
```
Passed! - Failed: 0, Passed: 214, Skipped: 0
```

### E2E Tests
All tests in both suites should pass:
- `full-flow.spec.ts`: 2 tests (Week 11, Week 12)
- `bowl-flow.spec.ts`: 3 tests (Complete flow, Admin upload UI, Validation)

## Known Good State

### Commit Before Bowl Games
Check the last passing E2E run before bowl games were added.

### Storage Paths Verified
```csharp
// Regular season (StorageService.cs)
const string LinesFolder = "lines";
// Path: lines/week-{week}-{year}.json

// Bowl games (StorageService.cs)  
const string BowlLinesFolder = "bowl-lines";
// Path: bowl-lines/bowls-{year}.json
```

## Contact
If E2E tests still fail after checking above:
1. Share CI logs from failing run
2. Share specific error messages
3. Share screenshots from test artifacts
