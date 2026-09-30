# Copilot Instructions for Against The Spread

## Project Overview

This is a Progressive Web Application (PWA) for managing a weekly college football pick'em game built with:
- **Frontend**: Blazor WebAssembly PWA
- **Backend**: Azure Functions (C#, isolated worker; SDK version pinned in `global.json`)
- **Storage**: Azure Blob Storage
- **Infrastructure**: Terraform
- **Testing**: xUnit, bUnit, NSubstitute, AwesomeAssertions, Playwright

## Development Guidelines

### Code Style & Standards

**C# Conventions:**
- Use PascalCase for public members and classes
- Use camelCase for private fields and local variables
- Interface prefix: `IServiceName`
- One class per file, matching the file name
- Use nullable reference types appropriately
- Add XML documentation for public APIs

**Naming:**
- Test methods: `MethodName_Scenario_ExpectedBehavior`
- Example: `GetLinesAsync_WithValidWeek_ReturnsWeeklyLines`
- Be descriptive and specific in names

### Testing Requirements

**Always follow Test-Driven Development (TDD):**
1. Write failing tests first
2. Implement minimum code to pass tests
3. Refactor and optimize
4. Update documentation

**CRITICAL: All tests must pass before any task is complete:**
- Unit tests: `dotnet test`
- E2E tests: `cd tests && npm test` (requires `./start-e2e.sh` running)
- Both test suites MUST pass before committing changes

**When to consider new E2E tests:**
- Adding new user-facing features
- Modifying existing user flows
- Changing API endpoints used by the UI
- Updating admin functionality

**Test Structure (AAA Pattern):**
```csharp
[Fact]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange - Set up test data and dependencies
    var service = new ExcelService();
    var stream = GetTestFileStream();
    
    // Act - Execute the method under test
    var result = service.ParseLinesFromExcel(stream);
    
    // Assert - Verify expected outcomes
    result.Should().NotBeNull();
    result.Week.Should().Be(1);
}
```

**Test Coverage Goals:**
- Core Library: >90%
- Functions: >80%
- Web Components: >70%

### End-to-End Testing with Playwright

**Playwright E2E tests validate the complete user flow including authentication:**
- Located in `tests/` directory with TypeScript
- Tests the full stack: Azurite → Functions → SWA CLI (mock auth) → Blazor Web App
- Uses SWA CLI mock authentication for admin routes
- Uploads test data via admin UI and validates Excel downloads
- Runs on every PR via GitHub Actions

**Key E2E Test Documentation:**
- `tests/README.md` - Complete E2E testing guide
- `tests/specs/full-flow.spec.ts` - Main test scenarios
- `tests/pages/admin-page.ts` - Admin page interactions
- `tests/pages/picks-page.ts` - Picks page interactions

**Running Playwright tests:**
```bash
# Start E2E environment (from repo root)
./start-e2e.sh

# Install dependencies (first time only)
cd tests
npm ci
npx playwright install chromium

# Run all tests
npm test

# Run with visible browser
npm run test:headed

# Debug tests interactively
npm run test:debug

# View test report
npm run test:report

# Stop E2E environment when done
cd .. && ./stop-e2e.sh
```

**E2E Test Requirements:**
- Start services with `./start-e2e.sh` (NOT `./start-local.sh`)
- `start-e2e.sh` configures Blazor app to route API calls through SWA CLI (port 4280)
- SWA CLI provides mock authentication at `/.auth/login/google`
- Ports: 10000 (Azurite), 7071 (Functions), 5158 (Web), 4280 (SWA CLI)
- Tests validate Excel format matches `reference-docs/Weekly Picks Example.xlsx`
- See `tests/README.md` for detailed documentation

### Build & Validation

**After each meaningful change, run:**
```bash
# Compile and check for errors
dotnet build

# Run all unit tests (156+ tests)
dotnet test

# Start E2E environment and run Playwright tests
./start-e2e.sh
cd tests && npm test
./stop-e2e.sh  # When done

# For regular local development (no SWA CLI needed)
./start-local.sh
```

**IMPORTANT: A coding task is NOT complete until:**
1. ✅ Code compiles without errors (`dotnet build`)
2. ✅ All unit tests pass (`dotnet test`)
3. ✅ All E2E tests pass (`cd tests && npm test`)
4. ✅ Documentation is updated if needed

**Do NOT commit code that:**
- Doesn't compile
- Has failing unit tests
- Has failing E2E tests
- Contains hardcoded credentials or secrets
- Lacks necessary tests for new features

### Project Structure

```
src/
├── AgainstTheSpread.Core/       # Shared models and services
│   ├── Models/                  # Domain models
│   ├── Interfaces/              # Service contracts
│   └── Services/                # Business logic
├── AgainstTheSpread.Functions/  # Azure Functions API
│   └── Program.cs              # DI configuration
├── AgainstTheSpread.Web/        # Blazor WASM PWA
│   ├── Pages/                  # Razor pages
│   ├── Components/             # Reusable components
│   └── Services/               # API client
└── AgainstTheSpread.Tests/      # Unit & integration tests
    ├── Models/                 # Model tests
    ├── Services/               # Service tests
    ├── Functions/              # API tests
    └── Web/                    # Component tests

tests/                           # E2E Playwright tests
├── specs/                      # Test specifications
├── helpers/                    # Test utilities
└── playwright.config.ts        # Playwright configuration
```

### Dependencies

**When adding new NuGet packages:**
- Only add if absolutely necessary
- Verify compatibility with the .NET SDK pinned in `global.json`
- Check for security vulnerabilities
- Update project documentation

**Common packages already in use:**
- EPPlus (Excel processing)
- Azure.Storage.Blobs
- NSubstitute (mocking)
- AwesomeAssertions (test assertions)
- bUnit (Blazor component testing)

### API Design

**Endpoints (User-facing only):**
- `GET /api/weeks?year={year}` - List available weeks
- `GET /api/lines/{week}?year={year}` - Get games for specific week
- `POST /api/picks` - Submit picks and generate Excel download

**Response patterns:**
- Return appropriate HTTP status codes (200, 400, 404, 500)
- Include descriptive error messages
- Use consistent JSON structure
- Handle exceptions gracefully

### Excel Format Requirements

**Critical:** Generated Excel files must EXACTLY match the format in `reference-docs/Weekly Picks Example.xlsx`:
- Row 1: Empty
- Row 2: Empty
- Row 3: Headers (Name, Pick 1, Pick 2, ..., Pick 6)
- Row 4: User data (name and 6 team picks)

### Blazor Component Guidelines

**For Blazor components:**
- Keep components small and focused
- Use dependency injection for services
- Handle loading and error states
- Make UI mobile-first (responsive design)
- Test components with bUnit

### Commit Messages

Follow Conventional Commits format:
```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:** `feat`, `fix`, `test`, `refactor`, `docs`, `chore`, `ci`

**Examples:**
```
feat(core): add Excel parsing service
test(functions): add unit tests for admin endpoints
fix(web): correct game selection validation
```

### Error Handling

**Always implement:**
- Input validation on all endpoints
- Null reference checks
- Try-catch blocks for external operations (blob storage, Excel processing)
- User-friendly error messages in UI
- Logging for debugging (Application Insights)

### Security Considerations

**Admin endpoints require Google authentication** (see `AUTHENTICATION_FIX.md`); weekly/bowl read and picks-generation APIs stay anonymous by design. Beyond that:
- Validate all inputs
- Set appropriate CORS policies
- Enforce file size limits
- Rate limiting on API endpoints
- No sensitive data in responses or logs

### Documentation

**Update documentation when:**
- Adding new features
- Changing API contracts
- Modifying configuration requirements
- Updating deployment process

**Key files to maintain:**
- `README.md` - Project overview and setup
- `CONTRIBUTING.md` - Development workflow
- `TESTING.md` - Testing strategy
- `.agents.md` - Agent development guide

### Performance

**Optimize for:**
- Mobile devices (PWA)
- Azure Functions consumption plan
- Blob storage operations (minimize round trips)
- Excel generation (streaming for large files)

### Deployment

**Environment awareness:**
- Local development uses Azurite and SWA CLI
- Dev environment for testing
- Production environment for live usage
- CI/CD via GitHub Actions on merge to main

**Infrastructure:**
- Managed via Terraform (infrastructure/terraform/)
- Do NOT manually modify Azure resources
- Update Terraform files for infrastructure changes

### Task Acceptance Criteria

**When completing a task:**
- All unit tests pass (`dotnet test`) - 156+ tests
- All E2E tests pass (`cd tests && npm test`) - 2+ tests
- Code compiles without warnings
- Documentation is updated
- Follows established patterns
- Includes error handling
- Mobile-friendly (for UI changes)
- Security considerations addressed

**For significant changes, consider:**
- Adding new E2E test scenarios in `tests/specs/`
- Updating Page Object Models in `tests/pages/`
- Updating `tests/README.md` if test setup changes

### Current Scope Focus

- Admin uploads weekly and bowl lines through the authenticated admin web UI
- Users select games and download Excel picks
- PWA installable on mobile devices
- Focus on core functionality, not enhancements

**Out of scope:**
- Pick history storage
- Automated scoring
- Leaderboards
- Push notifications

### Resources

**Reference files:**
- `reference-docs/Week 1 Lines.xlsx` - Sample input
- `reference-docs/Weekly Picks Example.xlsx` - Expected output format

**Documentation:**
- `.agents.md` - Comprehensive agent guide
- `TESTING.md` - Complete testing guide (unit tests)
- `CONTRIBUTING.md` - Contribution guidelines

**E2E Testing (Playwright):**
- `tests/README.md` - **Complete E2E testing guide** (START HERE)
- `tests/specs/full-flow.spec.ts` - Main E2E test scenarios
- `tests/pages/admin-page.ts` - Admin page object model
- `tests/pages/picks-page.ts` - Picks page object model
- `tests/helpers/` - Test utilities and validators
- `start-e2e.sh` / `stop-e2e.sh` - E2E environment scripts

### Common Pitfalls to Avoid

- ❌ Skipping tests
- ❌ Not running builds before committing
- ❌ Hardcoding values (use configuration)
- ❌ Ignoring mobile experience
- ❌ Breaking Excel format compatibility
- ❌ Adding unnecessary dependencies
- ❌ Creating large, unfocused PRs

### Success Indicators

- ✅ Tests written first (TDD)
- ✅ All tests pass
- ✅ Code compiles cleanly
- ✅ Documentation updated
- ✅ Follows existing patterns
- ✅ Mobile-friendly
- ✅ Security considered
- ✅ Small, focused commits

---

**Remember:** Build incrementally, test frequently, commit often. This project uses AI-assisted development with human code review.
