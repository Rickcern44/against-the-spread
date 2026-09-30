# Contributing to Against The Spread

Thank you for your interest in contributing! This project uses AI-assisted development with human code review.

## 🤖 AI-Assisted Development

This project is designed to be implemented primarily by AI agents with human oversight. Before contributing, please:

1. **Read [`.agents.md`](.agents.md)** - Complete guide for AI agents
2. **Follow the testing strategy** - Tests must be written first (TDD)
3. **Check frequently** - Compile and test after each change

## 📋 Development Workflow

### For AI Agents

1. **Review Context**
   - Read `.agents.md` for project structure and guidelines
   - Understand the current phase of development

2. **Implement Feature**
   - Write failing tests first (TDD)
   - Implement minimum code to pass tests
   - Refactor and optimize
   - Update documentation

3. **Validate Changes**
   - Run `dotnet build` - ensure compilation succeeds
   - Run `dotnet test` - ensure all unit tests pass (156+ tests)
   - Run E2E tests - `./start-e2e.sh && cd tests && npm test` (2+ tests)
   - Test locally with Azurite and browsers
   - Verify changes against acceptance criteria
   - **A task is NOT complete until ALL tests pass**

4. **Commit Changes**
   - Use clear, descriptive commit messages
   - Follow conventional commits format
   - Reference issue numbers where applicable

### For Human Reviewers

1. **Review Phase**
   - Check that tests were written first
   - Verify all tests pass
   - Review code quality and adherence to patterns
   - Test locally if needed
   - Provide feedback or approve

2. **Approval Criteria**
   - All tests passing
   - Code follows established patterns
   - Documentation updated
   - No security issues
   - Performance acceptable

## 🧪 Testing Requirements

All contributions MUST include tests:

**CRITICAL: All tests must pass before any PR is merged:**
- Unit tests: `dotnet test` (156+ tests)
- E2E tests: `cd tests && npm test` (2+ tests, requires `./start-e2e.sh`)

### Unit Tests
- Test all business logic
- Mock external dependencies
- Use AAA pattern (Arrange, Act, Assert)
- Clear test names describing scenario

Example:
```csharp
[Fact]
public void ParseLinesFromExcel_WithValidFile_ReturnsWeeklyLines()
{
    // Arrange
    var service = new ExcelService();
    var stream = GetTestFileStream();
    
    // Act
    var result = service.ParseLinesFromExcel(stream);
    
    // Assert
    result.Should().NotBeNull();
    result.Week.Should().Be(1);
}
```

### Integration Tests
- Test with Azurite (local Azure Storage)
- Test API endpoints end-to-end
- Validate file operations

### Component Tests (Blazor)
- Use bUnit for component testing
- Test user interactions
- Verify rendered output

### End-to-End Tests (Playwright)
- Validate complete user flows
- Test admin authentication and uploads
- Verify Excel download format
- See `tests/README.md` for complete guide
- Add new tests for significant UI/API changes

## 🏗️ Code Structure

### Project Organization
```
AgainstTheSpread.Core/
├── Models/              # Domain models
├── Interfaces/          # Service contracts
└── Services/            # Business logic

AgainstTheSpread.Functions/
├── Functions/           # HTTP triggers
└── Program.cs          # DI configuration

AgainstTheSpread.Web/
├── Pages/              # Razor pages
├── Components/         # Reusable components
└── Services/           # API client

AgainstTheSpread.Tests/
├── Models/             # Model tests
├── Services/           # Service tests
├── Functions/          # API tests
├── Web/                # Component tests
└── Integration/        # E2E tests
```

### Naming Conventions

**Tests:**
- `MethodName_Scenario_ExpectedBehavior`
- Example: `GetLinesAsync_WithValidWeek_ReturnsWeeklyLines`

**Classes:**
- PascalCase for public members
- Descriptive names
- Interface prefix: `IServiceName`

**Files:**
- Match class name
- One class per file

## 📝 Commit Message Format

Follow Conventional Commits:

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat`: New feature
- `fix`: Bug fix
- `test`: Adding tests
- `refactor`: Code refactoring
- `docs`: Documentation
- `chore`: Maintenance tasks
- `ci`: CI/CD changes

**Examples:**
```
feat(core): add Excel parsing service

Implements ExcelService with EPPlus to parse weekly lines
from uploaded Excel files. Includes comprehensive tests.

Closes #12

---

test(functions): add unit tests for admin endpoints

Adds tests for upload lines functionality with mocked
storage and Excel services.

---

fix(web): correct game selection validation

Fixes issue where users could select more than 6 games.
Adds validation and disables further selection.

Fixes #45
```

## 🔍 Code Review Checklist

Reviewers should verify:

- [ ] Tests written before implementation (TDD)
- [ ] All unit tests pass (`dotnet test` - 156+ tests)
- [ ] All E2E tests pass (`cd tests && npm test` - 2+ tests)
- [ ] Code compiles without warnings
- [ ] Documentation updated
- [ ] Follows established patterns
- [ ] No hardcoded values
- [ ] Error handling implemented
- [ ] Security considerations addressed
- [ ] Performance acceptable
- [ ] Mobile-friendly (for UI changes)
- [ ] New E2E tests added for significant UI changes

## 🚫 What NOT to Do

- ❌ Don't skip tests
- ❌ Don't commit code that doesn't compile
- ❌ Don't commit failing tests
- ❌ Don't hardcode credentials or secrets
- ❌ Don't make breaking changes without discussion
- ❌ Don't submit large PRs (break into smaller pieces)
- ❌ Don't ignore code review feedback

## ✅ What TO Do

- ✅ Write tests first
- ✅ Compile and test frequently
- ✅ Keep commits small and focused
- ✅ Update documentation
- ✅ Ask questions when unclear
- ✅ Reference issues in commits
- ✅ Be responsive to code review

## 📚 Required Reading

Before contributing:
1. [`.agents.md`](.agents.md) - Agent development guide
2. [`README.md`](README.md) - Project overview
3. [`TESTING.md`](TESTING.md) - Unit testing guide
4. [`tests/README.md`](tests/README.md) - **E2E testing guide (Playwright)**
5. Game rules in [`reference-docs/rules.md`](reference-docs/rules.md)

## 🐛 Reporting Issues

When reporting issues:
1. Use the issue template
2. Provide clear reproduction steps
3. Include error messages/stack traces
4. Specify environment (OS, .NET version, etc.)
5. Include relevant logs

## 💡 Suggesting Features

Feature requests should:
1. Describe the problem being solved
2. Propose a solution
3. Consider impact on existing features
4. Be aligned with project goals
5. Include acceptance criteria

## 🔐 Security

If you discover a security vulnerability:
1. **DO NOT** open a public issue
2. Email the maintainer directly
3. Provide detailed information
4. Wait for response before disclosure

## 📞 Getting Help

- Create an issue for bugs or questions
- Tag issues appropriately
- Be patient and respectful
- Provide context and examples

## 🙏 Thank You

Your contributions make this project better! We appreciate your time and effort in following these guidelines.

---

**Remember**: This is an AI-assisted project. The goal is for agents to implement features while humans review for quality and correctness.
