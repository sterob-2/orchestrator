# Refactoring Plan: Aider Learnings Application

Based on analysis of the Aider codebase (`/mnt/c/Users/robin/repos/aider`), this plan outlines improvements to address current issues (particularly issue #43: LLM not removing code) and enhance the orchestrator's reliability.

---

## Current Problem

**Issue #43**: Dev executor fails to remove methods despite clear instructions
- Spec says "Remove CreateBranchAsync and DeleteBranchAsync"
- Before/after examples provided in Interfaces section
- LLM returns different output (character count changes)
- **Methods still present in final output** (verified via grep)

**Root cause**: Prompting strategy doesn't use patterns that LLMs reliably understand for deletion.

---

## Phase 1: Fix Dev Prompting (IMMEDIATE - Addresses Issue #43)

### 1.1 Add Explicit Deletion Examples

**File**: `src/Orchestrator.App/Workflows/Prompts/DevPrompt.cs`

**Change**: Add concrete deletion examples to the prompt

```csharp
// Current system message - ADD TO IT:
var system = "You are a software engineer implementing a spec. " +
             "CRITICAL INSTRUCTIONS:\n" +
             "1. Read the Touch List Entry to understand what operation to perform\n" +
             "2. Study the Interfaces section which shows the required changes (before/after examples)\n" +
             "3. Apply those exact changes to the Current File Content\n" +
             "4. For 'Modify' operations: update/remove code as specified in the notes\n" +
             "5. Output ONLY the complete updated file content\n" +
             "6. Do NOT include before/after comments or explanations\n" +
             "7. Do NOT preserve code marked for removal\n" +
             // NEW INSTRUCTIONS:
             "8. When removing code: DELETE it entirely, do not comment it out\n" +
             "9. When you see 'Remove X' in instructions - X should NOT appear in your output\n" +
             "10. When modifying a method/class: Output the ENTIRE updated version\n" +
             "11. Do not use partial edits or assume unchanged parts remain\n" +
             "Follow the spec strictly.";

// ADD TO USER MESSAGE (after "=== YOUR TASK ==="):
builder.AppendLine();
builder.AppendLine("=== EXAMPLE: HOW TO REMOVE CODE ===");
builder.AppendLine("If the instruction says 'Remove CreateBranchAsync method':");
builder.AppendLine();
builder.AppendLine("BEFORE (current file content):");
builder.AppendLine("```csharp");
builder.AppendLine("public interface IFoo");
builder.AppendLine("{");
builder.AppendLine("    Task<bool> CreateBranchAsync(string name);  // ← TO BE REMOVED");
builder.AppendLine("    Task DeleteFileAsync(string path);");
builder.AppendLine("}");
builder.AppendLine("```");
builder.AppendLine();
builder.AppendLine("AFTER (your output should NOT include CreateBranchAsync):");
builder.AppendLine("```csharp");
builder.AppendLine("public interface IFoo");
builder.AppendLine("{");
builder.AppendLine("    Task DeleteFileAsync(string path);");
builder.AppendLine("}");
builder.AppendLine("```");
builder.AppendLine();
builder.AppendLine("Notice: CreateBranchAsync is COMPLETELY ABSENT from the output.");
builder.AppendLine();
```

**Aider reference**: `aider/coders/editblock_prompts.py` lines 58-69 shows this pattern

---

### 1.2 Add Validation Before Writing

**File**: `src/Orchestrator.App/Workflows/Executors/DevExecutor.cs`

**Change**: Validate LLM output before persisting

```csharp
// BEFORE writing (around line 108):
Logger.Debug($"[Dev] Validating LLM output for: {entry.Path}");
var validation = ValidateModification(entry, existing, updated);
if (!validation.Success)
{
    Logger.Warning($"[Dev] Validation failed for {entry.Path}: {validation.Reason}");
    return (false, $"Dev blocked: {validation.Reason}");
}

Logger.Debug($"[Dev] Validation passed, writing updated content to: {entry.Path}");
await FileOperationHelper.WriteAllTextAsync(WorkContext, entry.Path, updated);

// ADD NEW METHOD:
private ValidationResult ValidateModification(
    TouchListEntry entry,
    string? existing,
    string updated)
{
    if (entry.Operation != TouchOperation.Modify)
    {
        return ValidationResult.Success();
    }

    // Parse removal requirements from notes
    // Example: "Remove CreateBranchAsync and DeleteBranchAsync methods"
    var removePatterns = new[] { "remove", "delete", "eliminate" };
    var notesLower = entry.Notes.ToLowerInvariant();

    if (removePatterns.Any(p => notesLower.Contains(p)))
    {
        // Extract what should be removed (simplified - can be enhanced)
        var words = entry.Notes.Split(new[] { ' ', ',', '.', ';' },
            StringSplitOptions.RemoveEmptyEntries);

        // Check for method/class names in updated output
        foreach (var word in words)
        {
            // Skip common words
            if (word.Length < 5 || removePatterns.Contains(word.ToLowerInvariant()))
                continue;

            // If this looks like a C# identifier and notes say to remove it
            if (char.IsUpper(word[0]) && updated.Contains(word))
            {
                // Check if this is actually something to remove
                if (notesLower.Contains($"remove {word.ToLowerInvariant()}") ||
                    notesLower.Contains($"delete {word.ToLowerInvariant()}"))
                {
                    return ValidationResult.Fail(
                        $"'{word}' should be removed but is still present in output");
                }
            }
        }
    }

    return ValidationResult.Success();
}

// ADD HELPER CLASS:
private record ValidationResult(bool Success, string Reason = "")
{
    public static ValidationResult Success() => new(true);
    public static ValidationResult Fail(string reason) => new(false, reason);
}
```

---

### 1.3 Add Reflection Loop (Retry with Feedback)

**File**: `src/Orchestrator.App/Workflows/Executors/DevExecutor.cs`

**Change**: Add retry logic when validation fails

```csharp
// REPLACE the LLM call section with retry loop:
const int MAX_RETRIES = 2;
string updated = "";

for (int retry = 0; retry <= MAX_RETRIES; retry++)
{
    Logger.Debug($"[Dev] Calling LLM for: {entry.Path} (attempt {retry + 1})");

    var prompt = retry == 0
        ? DevPrompt.Build(mode, parsedSpec, entry, existing)
        : DevPrompt.BuildRetry(mode, parsedSpec, entry, existing, lastError);

    updated = await CallLlmAsync(
        WorkContext.Config.DevModel,
        prompt.System,
        prompt.User,
        cancellationToken);

    Logger.Debug($"[Dev] LLM response received for: {entry.Path} (length: {updated?.Length ?? 0})");

    if (string.IsNullOrWhiteSpace(updated))
    {
        Logger.Warning($"[Dev] Empty LLM output for: {entry.Path}");
        return (false, $"Dev blocked: empty output for {entry.Path}.");
    }

    // Validate
    var validation = ValidateModification(entry, existing, updated);

    if (validation.Success)
    {
        Logger.Info($"[Dev] Validation passed on attempt {retry + 1}");
        break; // Success!
    }

    if (retry < MAX_RETRIES)
    {
        lastError = validation.Reason;
        Logger.Warning($"[Dev] Attempt {retry + 1} failed: {lastError}. Retrying with feedback...");
    }
    else
    {
        Logger.Error($"[Dev] All {MAX_RETRIES + 1} attempts failed for {entry.Path}");
        return (false, $"Dev blocked: {validation.Reason} (after {MAX_RETRIES + 1} attempts)");
    }
}
```

**Add new BuildRetry method to DevPrompt.cs**:
```csharp
public static (string System, string User) BuildRetry(
    string mode,
    ParsedSpec spec,
    TouchListEntry entry,
    string? existingContent,
    string errorFeedback)
{
    var (system, user) = Build(mode, spec, entry, existingContent);

    // Add error feedback
    var retryUser = user + $"\n\n" +
        $"=== PREVIOUS ATTEMPT FAILED ===\n" +
        $"Error: {errorFeedback}\n\n" +
        $"Please fix the issue and provide the complete updated file content:\n";

    return (system, retryUser);
}
```

**Aider reference**: `aider/coders/editblock_coder.py` lines 84-124 shows reflection loop pattern

---

### 1.4 Enhanced Debug Logging (Already Partially Done)

**File**: `src/Orchestrator.App/Workflows/Executors/DevExecutor.cs`

**Expand existing debug code** (around lines 113-123):

```csharp
// Expand the existing debug section:
if (entry.Path.Contains("IGitHubClient.cs") || entry.Path.Contains("OctokitGitHubClient.cs"))
{
    var hasCreateBranch = updated.Contains("CreateBranchAsync");
    var hasDeleteBranch = updated.Contains("DeleteBranchAsync");
    Logger.Warning($"[Dev] Method check for {entry.Path}: CreateBranchAsync={hasCreateBranch}, DeleteBranchAsync={hasDeleteBranch}");

    if (hasCreateBranch || hasDeleteBranch)
    {
        Logger.Warning($"[Dev] LLM FAILED to remove methods from {entry.Path}!");
        // Save problematic output for debugging
        var debugPath = $"orchestrator/debug/failed-{entry.Path.Replace('/', '-')}";
        await FileOperationHelper.WriteAllTextAsync(WorkContext, debugPath, updated);
        Logger.Warning($"[Dev] Saved failed output to: {debugPath}");
    }
}
```

---

## Phase 2: Edit Application Robustness (SHORT-TERM)

### 2.1 Multi-Strategy Matching

**New file**: `src/Orchestrator.App/Workflows/EditApplicationService.cs`

```csharp
namespace Orchestrator.App.Workflows;

/// <summary>
/// Applies LLM edits with multiple fallback strategies for robustness.
/// Based on Aider's search_replace.py approach.
/// </summary>
internal class EditApplicationService
{
    public (bool Success, string Content, string ErrorMessage) ApplyEdit(
        string original,
        string llmOutput,
        TouchOperation operation)
    {
        // Strategy 1: Direct replacement (LLM output is perfect)
        if (IsValidOutput(llmOutput, operation))
        {
            return (true, llmOutput, "");
        }

        // Strategy 2: Strip blank lines
        var normalized = StripBlankLines(llmOutput);
        if (IsValidOutput(normalized, operation))
        {
            Logger.Debug("[EditApp] Applied edit with blank line stripping");
            return (true, normalized, "");
        }

        // Strategy 3: Fix indentation (LLM often gets leading whitespace wrong)
        var reindented = FixIndentation(original, llmOutput);
        if (IsValidOutput(reindented, operation))
        {
            Logger.Debug("[EditApp] Applied edit with indentation fix");
            return (true, reindented, "");
        }

        // Strategy 4: Normalize line endings
        var normalizedLineEndings = NormalizeLineEndings(llmOutput);
        if (IsValidOutput(normalizedLineEndings, operation))
        {
            Logger.Debug("[EditApp] Applied edit with line ending normalization");
            return (true, normalizedLineEndings, "");
        }

        // All strategies failed
        return (false, "", "LLM output could not be validated or normalized");
    }

    private bool IsValidOutput(string output, TouchOperation operation)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(output))
            return false;

        // For C#, check basic syntax validity
        if (operation == TouchOperation.Add || operation == TouchOperation.Modify)
        {
            // Check for balanced braces (simple heuristic)
            var openBraces = output.Count(c => c == '{');
            var closeBraces = output.Count(c => c == '}');
            if (openBraces != closeBraces)
            {
                Logger.Debug($"[EditApp] Validation failed: unbalanced braces ({openBraces} open, {closeBraces} close)");
                return false;
            }
        }

        return true;
    }

    private string StripBlankLines(string content)
    {
        var lines = content.Split('\n');
        var nonBlank = lines.Where(line => !string.IsNullOrWhiteSpace(line));
        return string.Join('\n', nonBlank);
    }

    private string FixIndentation(string original, string llmOutput)
    {
        // Detect common leading whitespace errors
        var originalLines = original.Split('\n');
        var outputLines = llmOutput.Split('\n');

        // Find minimum indentation in both
        var minOriginal = FindMinIndentation(originalLines);
        var minOutput = FindMinIndentation(outputLines);

        if (minOriginal != minOutput && minOutput > 0)
        {
            // Adjust output indentation
            var adjusted = outputLines.Select(line =>
            {
                if (line.Length > minOutput)
                    return line.Substring(minOutput);
                return line;
            });
            return string.Join('\n', adjusted);
        }

        return llmOutput;
    }

    private int FindMinIndentation(string[] lines)
    {
        return lines
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Length - line.TrimStart().Length)
            .DefaultIfEmpty(0)
            .Min();
    }

    private string NormalizeLineEndings(string content)
    {
        return content.Replace("\r\n", "\n").Replace("\r", "\n");
    }
}
```

**Aider reference**: `aider/coders/search_replace.py` lines 565-608

---

### 2.2 Integration into DevExecutor

**File**: `src/Orchestrator.App/Workflows/Executors/DevExecutor.cs`

```csharp
// Add field
private readonly EditApplicationService _editService = new();

// Use in apply section:
var (success, finalContent, error) = _editService.ApplyEdit(
    existing ?? "",
    updated,
    entry.Operation);

if (!success)
{
    Logger.Warning($"[Dev] Edit application failed for {entry.Path}: {error}");
    return (false, $"Dev blocked: {error}");
}

await FileOperationHelper.WriteAllTextAsync(WorkContext, entry.Path, finalContent);
```

---

## Phase 3: Testing Infrastructure (MEDIUM-TERM)

### 3.1 Git Temporary Workspace Helper

**New file**: `tests/TestHelpers/GitTemporaryWorkspace.cs`

```csharp
using System;
using System.IO;
using LibGit2Sharp;

namespace Orchestrator.App.Tests.TestHelpers;

/// <summary>
/// Creates a temporary workspace with initialized git repo for testing.
/// Based on Aider's GitTemporaryDirectory pattern.
/// </summary>
public sealed class GitTemporaryWorkspace : IDisposable
{
    public string Path { get; }
    public IRepoWorkspace Workspace { get; }
    public IRepoGit Repo { get; }

    public GitTemporaryWorkspace()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"test-orchestrator-{Guid.NewGuid()}");

        Directory.CreateDirectory(Path);

        // Initialize git repo
        Repository.Init(Path);

        // Configure git user (required for commits)
        using var repo = new Repository(Path);
        repo.Config.Set("user.name", "Test User");
        repo.Config.Set("user.email", "test@example.com");

        Workspace = new RepoWorkspace(Path);
        Repo = new RepoGit(Path);
    }

    public void WriteFile(string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        var dir = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
    }

    public string ReadFile(string relativePath)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        return File.ReadAllText(fullPath);
    }

    public void CommitAll(string message)
    {
        using var repo = new Repository(Path);
        Commands.Stage(repo, "*");
        var signature = new Signature("Test User", "test@example.com", DateTimeOffset.Now);
        repo.Commit(message, signature, signature);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                // Force delete (handle Windows file locks)
                Directory.Delete(Path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Ignore cleanup errors (common on Windows)
        }
        catch (UnauthorizedAccessException)
        {
            // Ignore cleanup errors
        }
    }
}
```

**Aider reference**: `aider/utils.py` GitTemporaryDirectory

---

### 3.2 Mock LLM Client Builder

**New file**: `tests/TestHelpers/MockLlmClientBuilder.cs`

```csharp
using Moq;
using Orchestrator.App.Core.Interfaces;

namespace Orchestrator.App.Tests.TestHelpers;

public static class MockLlmClientBuilder
{
    public static Mock<ILlmClient> WithResponse(string content)
    {
        var mock = new Mock<ILlmClient>();
        mock.Setup(m => m.CallAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(content);
        return mock;
    }

    public static Mock<ILlmClient> WithResponses(params string[] responses)
    {
        var mock = new Mock<ILlmClient>();
        var sequence = mock.SetupSequence(m => m.CallAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()));

        foreach (var response in responses)
            sequence.ReturnsAsync(response);

        return mock;
    }

    public static Mock<ILlmClient> ThatFails(Exception ex)
    {
        var mock = new Mock<ILlmClient>();
        mock.Setup(m => m.CallAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(ex);
        return mock;
    }

    public static Mock<ILlmClient> WithValidationRetry(
        string failedResponse,
        string successResponse)
    {
        return WithResponses(failedResponse, successResponse);
    }
}
```

---

### 3.3 Integration Test Example

**New file**: `tests/Integration/DevExecutorIntegrationTests.cs`

```csharp
using Xunit;
using Orchestrator.App.Tests.TestHelpers;

namespace Orchestrator.App.Tests.Integration;

public class DevExecutorIntegrationTests
{
    [Fact]
    public async Task DevExecutor_RemovesMethods_WhenSpecified()
    {
        using var workspace = new GitTemporaryWorkspace();

        // Setup: Create file with methods to remove
        workspace.WriteFile("src/IFoo.cs", @"
namespace Test;

public interface IFoo
{
    Task<bool> CreateBranchAsync(string name);
    Task DeleteBranchAsync(string name);
    Task KeepThisMethod();
}");
        workspace.CommitAll("Initial commit");

        // Mock LLM to return file without removed methods
        var mockLlm = MockLlmClientBuilder.WithResponse(@"
namespace Test;

public interface IFoo
{
    Task KeepThisMethod();
}");

        // Create spec that says to remove methods
        var spec = """
            ## Touch List
            | Operation | Path | Notes |
            |-----------|------|-------|
            | Modify | src/IFoo.cs | Remove CreateBranchAsync and DeleteBranchAsync methods |

            ## Interfaces
            Before:
            ```csharp
            Task<bool> CreateBranchAsync(string name);
            Task DeleteBranchAsync(string name);
            ```
            After:
            ```csharp
            // Methods removed
            ```
            """;

        workspace.WriteFile("orchestrator/specs/issue-1.md", spec);

        // Execute Dev stage
        var config = MockWorkContext.CreateConfig();
        var workItem = new WorkItem(1, "Remove methods", "Remove unused methods", "url", []);
        var context = new WorkContext(
            workItem,
            new Mock<IGitHubClient>().Object,
            config,
            workspace.Workspace,
            workspace.Repo,
            mockLlm.Object);

        var executor = new DevExecutor(context, config.Workflow);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", workspace.Path, workspace.Path, "owner", "user", 1),
            Mode: null,
            Attempt: 0);

        var workflowContext = new Mock<IWorkflowContext>();
        workflowContext.Setup(c => c.QueueStateUpdateAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        // Act
        var result = await executor.HandleAsync(input, workflowContext.Object, CancellationToken.None);

        // Assert
        Assert.True(result.Success);

        var updatedContent = workspace.ReadFile("src/IFoo.cs");
        Assert.DoesNotContain("CreateBranchAsync", updatedContent);
        Assert.DoesNotContain("DeleteBranchAsync", updatedContent);
        Assert.Contains("KeepThisMethod", updatedContent);
    }
}
```

---

## Phase 4: Architecture Enhancements (LONG-TERM)

### 4.1 Repository Mapping Service

**Purpose**: Provide LLM with codebase structure context (similar to Aider's repomap)

**New file**: `src/Orchestrator.App/Workflows/RepoMapService.cs`

```csharp
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Orchestrator.App.Workflows;

/// <summary>
/// Generates AST-based repository map for LLM context.
/// Based on Aider's repomap.py using tree-sitter.
/// We use Roslyn for C# parsing.
/// </summary>
internal class RepoMapService
{
    public async Task<string> GenerateRepoMap(
        IRepoWorkspace workspace,
        int maxTokens = 2000)
    {
        var map = new StringBuilder();
        map.AppendLine("=== REPOSITORY MAP ===");

        // Get all .cs files
        var files = Directory.GetFiles(workspace.Root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/bin/") && !f.Contains("/obj/"))
            .Take(50); // Limit for performance

        foreach (var file in files)
        {
            var relativePath = Path.GetRelativePath(workspace.Root, file);
            var content = File.ReadAllText(file);

            // Parse with Roslyn
            var tree = CSharpSyntaxTree.ParseText(content);
            var root = await tree.GetRootAsync();

            // Extract classes and interfaces
            var types = root.DescendantNodes()
                .OfType<TypeDeclarationSyntax>()
                .Select(t => $"  {t.Keyword} {t.Identifier}");

            // Extract public methods
            var methods = root.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(m => m.Modifiers.Any(SyntaxKind.PublicKeyword))
                .Select(m => $"    {m.Identifier}");

            if (types.Any() || methods.Any())
            {
                map.AppendLine($"{relativePath}:");
                foreach (var type in types)
                    map.AppendLine(type);
                foreach (var method in methods)
                    map.AppendLine(method);
                map.AppendLine();
            }

            // Check token budget (rough estimate: ~4 chars per token)
            if (map.Length / 4 > maxTokens)
                break;
        }

        return map.ToString();
    }
}
```

**Integration**: Add to TechLead and Dev prompts

**Aider reference**: `aider/repomap.py`

---

### 4.2 Hierarchical Configuration

**Enhancement**: `src/Orchestrator.App/Core/Configuration/OrchestratorConfig.cs`

```csharp
public static OrchestratorConfig LoadHierarchical()
{
    var configs = new List<OrchestratorConfig>();

    // 1. Home directory config
    var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    var homeConfig = LoadFromDirectory(Path.Combine(homeDir, ".orchestrator"));
    if (homeConfig != null) configs.Add(homeConfig);

    // 2. Git root config (find .git directory)
    var gitRoot = FindGitRoot(Directory.GetCurrentDirectory());
    if (gitRoot != null)
    {
        var gitConfig = LoadFromDirectory(Path.Combine(gitRoot, "orchestrator"));
        if (gitConfig != null) configs.Add(gitConfig);
    }

    // 3. Current directory config
    var cwdConfig = LoadFromDirectory("orchestrator");
    if (cwdConfig != null) configs.Add(cwdConfig);

    // 4. Environment variables (highest priority)
    var envConfig = FromEnvironment();
    configs.Add(envConfig);

    // Merge: later configs override earlier
    return MergeConfigs(configs);
}

private static string? FindGitRoot(string startPath)
{
    var current = new DirectoryInfo(startPath);
    while (current != null)
    {
        if (Directory.Exists(Path.Combine(current.FullName, ".git")))
            return current.FullName;
        current = current.Parent;
    }
    return null;
}
```

**Aider reference**: `aider/main.py` lines 305-332

---

### 4.3 Enhanced Exception Handling

**New file**: `src/Orchestrator.App/Exceptions/LlmExceptions.cs`

```csharp
namespace Orchestrator.App.Exceptions;

public record ExceptionPolicy(
    Type ExceptionType,
    bool ShouldRetry,
    string UserFriendlyMessage,
    Func<Exception, string>? DetailExtractor = null);

public static class LlmExceptionHandler
{
    private static readonly ExceptionPolicy[] Policies = {
        new(typeof(HttpRequestException), true,
            "Network error connecting to LLM API"),
        new(typeof(TaskCanceledException), true,
            "LLM request timed out"),
        new(typeof(UnauthorizedAccessException), false,
            "Invalid API key or unauthorized"),
        new(typeof(ArgumentException), false,
            "Invalid request parameters"),
    };

    public static async Task<T> ExecuteWithRetry<T>(
        Func<Task<T>> action,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var delay = TimeSpan.FromMilliseconds(125);

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < maxRetries)
            {
                var policy = Policies.FirstOrDefault(p => p.ExceptionType.IsAssignableFrom(ex.GetType()));

                if (policy == null || !policy.ShouldRetry)
                    throw;

                Logger.Warning($"{policy.UserFriendlyMessage}: {ex.Message}");
                Logger.Info($"Retrying in {delay.TotalSeconds}s... (attempt {attempt + 1}/{maxRetries})");

                await Task.Delay(delay, cancellationToken);
                delay *= 2; // Exponential backoff
            }
        }

        throw new InvalidOperationException("Retry logic failed to execute");
    }
}
```

**Aider reference**: `aider/exceptions.py` + retry logic in `base_coder.py`

---

## Success Metrics

1. **Issue #43 resolved**: Methods removed when spec says "remove" (95%+ success rate)
2. **Dev stage reliability**: First-attempt success rate >90%
3. **Reflection loop usage**: <10% of Dev executions require retry
4. **Test coverage**: 80%+ overall, 90%+ for new components
5. **False removals**: 0% (never removes code that should stay)

---

## Implementation Priority

### Immediate (Week 1)
- [ ] Phase 1.1: Add explicit deletion examples to DevPrompt
- [ ] Phase 1.2: Add validation before writing
- [ ] Phase 1.3: Add reflection loop with retries
- [ ] Test with issue #43

### Short-term (Week 2-3)
- [ ] Phase 2.1: Create EditApplicationService
- [ ] Phase 2.2: Integrate into DevExecutor
- [ ] Phase 3.1: Create GitTemporaryWorkspace helper
- [ ] Phase 3.2: Create MockLlmClientBuilder
- [ ] Phase 3.3: Add integration tests for Dev executor

### Medium-term (Month 1-2)
- [ ] Phase 4.1: Implement RepoMapService
- [ ] Phase 4.2: Add hierarchical config loading
- [ ] Phase 4.3: Enhance exception handling
- [ ] Reorganize tests into Unit/ and Integration/

### Long-term (Month 2+)
- [ ] Add tree-sitter support for multi-language parsing
- [ ] Implement diff-match-patch for fuzzy matching
- [ ] Create comprehensive test fixture library
- [ ] Add metrics dashboard for success rates

---

## Key Learnings from Aider

1. **Explicit > Implicit**: Show concrete deletion examples, don't assume LLM infers
2. **Validate Early**: Check output before persisting, catch errors fast
3. **Reflection Loops**: Give LLM specific feedback on failures, allow retry
4. **Multi-Strategy**: Try multiple normalization approaches (whitespace, indentation)
5. **Pragmatic Testing**: Use real filesystem/git in tests, mock only LLM calls
6. **Token Budget Awareness**: Truncate context intelligently, prioritize relevant code
7. **Graceful Degradation**: Always have fallback strategies

---

## Aider Source References

| Topic | File Reference |
|-------|---------------|
| Deletion prompts | `aider/coders/editblock_prompts.py` lines 58-69 |
| Reflection loop | `aider/coders/editblock_coder.py` lines 84-124 |
| Multi-strategy matching | `aider/coders/search_replace.py` lines 565-608 |
| Exception handling | `aider/exceptions.py` + `base_coder.py` lines 1449-1512 |
| Testing utilities | `aider/utils.py` GitTemporaryDirectory |
| Repository mapping | `aider/repomap.py` |
| Config hierarchy | `aider/main.py` lines 305-332 |
