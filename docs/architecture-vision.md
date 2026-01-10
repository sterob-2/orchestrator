# Architecture Vision: Orchestrator

## Architecture Philosophy

**"Boring technology, clean boundaries, minimal abstractions."**

We prioritize maintainability and clarity over cleverness. Code should be obvious. If it needs comments to explain, it's too complex.

## Tech Stack (Frozen)

### Core Platform
- **.NET 8.0** (LTS)
- **C# 12** language features
- **xUnit** for testing
- **Moq** for mocking
- **GitHub API** (Octokit)
- **OpenAI API** (GPT-5 / GPT-5-mini)

### Infrastructure
- **Docker** for MCP server hosting
- **Git** for version control
- **GitHub Issues** for work tracking
- **GitHub Projects** for kanban boards

### DO NOT ADD without explicit approval:
- No new frameworks
- No new database systems
- No new container orchestrators
- No new LLM providers

## Folder Structure (Enforced)

```
src/Orchestrator.App/
├── Core/
│   ├── Configuration/    # OrchestratorConfig, WorkflowConfig
│   ├── Models/           # WorkItem, RefinementResult, WorkContext
│   └── Interfaces/       # IGitHubClient, ILlmClient, IRepoGit
├── Infrastructure/
│   ├── Filesystem/       # RepoWorkspace
│   ├── Git/              # RepoGit
│   ├── GitHub/           # OctokitGitHubClient
│   ├── Llm/              # LlmClient
│   └── Mcp/              # McpClientManager
├── Workflows/
│   ├── Executors/        # RefinementExecutor, DevExecutor, etc.
│   ├── Gates/            # DorGateValidator, SpecGateValidator
│   └── Prompts/          # RefinementPrompt, TechLeadPrompt
├── Parsing/              # Markdown parsers
└── Utilities/            # Helpers (minimal)

tests/
├── Unit/                 # Fast, isolated tests
├── Integration/          # Multi-component tests
└── TestHelpers/          # Shared test infrastructure
```

**Rule:** Every new file goes in the correct folder. No "miscellaneous" or "helpers" dumping grounds.

## Design Patterns (Use These)

### 1. Records for DTOs
```csharp
// ✅ DO: Immutable records
public sealed record WorkItem(
    int Number,
    string Title,
    string Body,
    string Url,
    IReadOnlyList<string> Labels
);

// ❌ DON'T: Mutable classes
public class WorkItem
{
    public int Number { get; set; }
    public string Title { get; set; }
}
```

### 2. Interface Segregation
```csharp
// ✅ DO: Small, focused interfaces
public interface IRepoGit
{
    void EnsureBranch(string branchName, string baseBranch);
    bool CommitAndPush(string branchName, string message, IEnumerable<string> paths);
}

// ❌ DON'T: God interfaces with 20+ methods
```

### 3. Dependency Injection via Constructor
```csharp
// ✅ DO: Explicit dependencies
public sealed class DevExecutor
{
    private readonly WorkContext _context;
    public DevExecutor(WorkContext context) => _context = context;
}

// ❌ DON'T: Service locators, static dependencies
```

### 4. Fail Fast with Exceptions
```csharp
// ✅ DO: Clear error messages
if (string.IsNullOrEmpty(config.RepoOwner))
    throw new InvalidOperationException("RepoOwner cannot be empty");

// ❌ DON'T: Silent failures, null returns for errors
```

## Anti-Patterns (Forbidden)

### ❌ God Objects
```csharp
// Current WorkContext has 9 dependencies - this is the limit!
// DO NOT add more dependencies to WorkContext
// DO NOT create new God Objects
```

### ❌ Premature Abstraction
```csharp
// ❌ DON'T: Generic base classes for single use
public abstract class BaseValidator<T> { }

// ✅ DO: Concrete implementations, extract common code only after 3+ uses
public sealed class DorGateValidator { }
```

### ❌ Configuration Overload
```csharp
// ❌ DON'T: Config for every possible scenario
public class McpConfig
{
    public int MaxRetries { get; set; }
    public int RetryDelayMs { get; set; }
    public string RetryStrategy { get; set; }
    public bool EnableCircuitBreaker { get; set; }
    // ... 20 more properties
}

// ✅ DO: Hard-code sane defaults, config only for environment-specific values
public class McpConfig
{
    public string ServerPath { get; init; }
    // Retries = 3, delay = 1000ms (hard-coded)
}
```

### ❌ Speculative Features
```csharp
// ❌ DON'T: Interfaces/abstractions for single implementation
public interface IHealthCheckStrategy { }
public class BasicHealthCheck : IHealthCheckStrategy { }

// ✅ DO: Concrete class, add interface when second implementation needed
public sealed class HealthCheck { }
```

### ❌ Layer Violations
```csharp
// ❌ DON'T: Infrastructure calling Workflows
// Infrastructure/ → Workflows/ ✗

// ❌ DON'T: Workflows calling Infrastructure concrete types
// Use interfaces from Core/Interfaces/

// ✅ DO: Clean dependency flow
// Workflows → Core ← Infrastructure
```

## Code Generation Guidelines

### When LLM generates code:

**1. Start Minimal**
- Implement ONLY what's in acceptance criteria
- Hard-code before adding configuration
- No error handling for impossible scenarios
- No logging unless debugging is needed

**2. Follow Existing Patterns**
```csharp
// Find similar code in codebase
// Copy its structure
// Adapt to new requirements
// DO NOT invent new patterns
```

**3. Test Coverage**
- One test per acceptance criterion
- Happy path required
- Edge cases only if specified in AC
- Integration test for end-to-end flow

**4. No Future-Proofing**
```csharp
// ❌ DON'T:
public async Task ProcessAsync(IEnumerable<WorkItem> items,
    CancellationToken cancellationToken,
    IProgress<ProcessingProgress>? progress = null,
    ProcessingOptions? options = null)

// ✅ DO: (if processing one item)
public async Task ProcessAsync(WorkItem item)
```

## File Size Limits

**Guidance:**
- Executors: 200-400 LOC max
- Validators: 100-200 LOC max
- Prompts: 150-300 LOC max
- Models: 50-100 LOC max

**If exceeded:** Split the class, don't justify bloat

## Testing Strategy

### Unit Tests (Fast, Isolated)
- Mock all external dependencies
- Test business logic only
- Arrange-Act-Assert pattern
- One assertion per test (preferred)

### Integration Tests (Slow, E2E)
- Use `TempWorkspace` for file system
- Use `FakeGitHubClient`, `FakeRepoGit` fakes
- Test workflow stage transitions
- Verify file outputs

**Coverage Target:** 80% line coverage, 100% of acceptance criteria

## Documentation Standards

### Code Comments: Minimal
```csharp
// ❌ DON'T: Explain what code does
// Loop through items and process each one
foreach (var item in items) { }

// ✅ DO: Explain WHY if non-obvious
// Reset attempt counter when human provides answer (prevent infinite loops)
await context.QueueStateUpdateAsync("attempt:Refinement", 0);
```

### XML Docs: Only for public APIs
```csharp
// ✅ DO: Document public interfaces
/// <summary>
/// Validates Definition of Ready criteria for refined work items.
/// </summary>
public sealed class DorGateValidator { }

// ❌ DON'T: Document private/internal methods
```

### README: Keep Updated
- Update when adding new executors
- Update when changing workflow stages
- Update when adding new configuration

## Performance Guidelines

**Optimization Priority:**
1. Correctness
2. Clarity
3. Maintainability
4. Performance (only if measured need)

**DO NOT:**
- Pre-optimize
- Cache unless proven bottleneck
- Use async unless I/O-bound
- Add complexity for theoretical gains

## Decision Flowchart

```
Need new feature?
  ↓
Is there existing code that does 80% of this?
  ↓ Yes
  Extend existing code
  ↓ No
  ↓
Can it be < 100 LOC?
  ↓ Yes
  Write simple implementation
  ↓ No
  ↓
Can you split into smaller features?
  ↓ Yes
  Create multiple issues
  ↓ No
  ↓
Document WHY it's complex, get review approval
```

## Examples

### ✅ Good Code (Minimal, Clear)
```csharp
public sealed record HealthCheckResult(bool IsHealthy, string Message);

public sealed class HealthCheck
{
    private readonly HttpClient _client;

    public HealthCheck(HttpClient client) => _client = client;

    public async Task<HealthCheckResult> CheckAsync()
    {
        try
        {
            var response = await _client.GetAsync("/health");
            return new HealthCheckResult(
                response.IsSuccessStatusCode,
                $"Status: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(false, ex.Message);
        }
    }
}
```

### ❌ Bad Code (Over-Engineered)
```csharp
public interface IHealthCheckStrategy { }
public interface IHealthCheckResultFormatter { }
public interface IHealthCheckCache { }

public sealed class HealthCheckOrchestrator
{
    private readonly IEnumerable<IHealthCheckStrategy> _strategies;
    private readonly IHealthCheckResultFormatter _formatter;
    private readonly IHealthCheckCache _cache;
    private readonly ILogger _logger;
    private readonly HealthCheckOptions _options;

    // ... 200 lines of abstractions for simple GET /health
}
```

## Alignment with Product Vision

Every architecture decision supports:
- **Quality Over Speed:** Clean boundaries, testable code
- **Minimal First:** No abstractions until needed
- **Small Issues:** Changes are localized, easy to review
- **Human Oversight:** Code is readable, reviewable in 15 minutes
