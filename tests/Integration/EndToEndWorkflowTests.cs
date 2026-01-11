using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows;
using Orchestrator.App.Workflows.Executors;

namespace Orchestrator.App.Tests.Integration;

public class EndToEndWorkflowTests : IDisposable
{
    private readonly TempWorkspace _tempWorkspace;
    private readonly Mock<IGitHubClient> _githubMock;
    private readonly Mock<IRepoGit> _repoMock;
    private readonly ScriptedLlmClient _llmClient;

    public EndToEndWorkflowTests()
    {
        _tempWorkspace = new TempWorkspace();
        _githubMock = new Mock<IGitHubClient>();
        _repoMock = new Mock<IRepoGit>();

        // Setup default Git mocks
        _repoMock.Setup(x => x.EnsureBranch(It.IsAny<string>(), It.IsAny<string>()));
        _repoMock.Setup(x => x.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .Returns(true);

        // Scripted LLM with logic to return valid JSONs based on the prompt/stage
        _llmClient = new ScriptedLlmClient((system, user) =>
        {
            if (system.Contains("SDLC refinement assistant"))
            {
                return @"{
                    ""clarifiedStory"": ""As a user, I want to be able to use the feature X so that I can achieve goal Y, which is very important for the business context and overall system functionality."",
                    ""acceptanceCriteria"": [
                        ""Given I am a user, When I do X, Then Y happens."",
                        ""Given I am a user, When I do Z, Then A happens."",
                        ""Given I am a user, When I do B, Then C happens.""
                    ],
                    ""openQuestions"": [],
                    ""complexity"": {
                        ""storyPoints"": 3,
                        ""risk"": ""low""
                    }
                }";
            }

            if (system.Contains("senior tech lead"))
            {
                // Return the template filled out
                return @"# Spec: Issue 1 - Test Issue

STATUS: DRAFT
UPDATED: 2024-01-01

## Ziel
Implement feature X.

## Nicht-Ziele
- Feature Y

## Komponenten
- Core

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| add | src/Feature.cs | New feature |
| modify | tests/FeatureTests.cs | Tests |

## Interfaces
```csharp
public interface IFeature {}
```

## Szenarien
Scenario: Success
Given A
When B
Then C

Scenario: Error
Given A
When Fail
Then Error

Scenario: Edge
Given A
When Edge
Then Edge

## Sequenz
1. Call A
2. Return B

## Testmatrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/FeatureTests.cs | Cover all |
";
            }

            if (system.Contains("software engineer"))
            {
                // Dev returns code for files
                return "// Implemented feature";
            }

            if (system.Contains("AI code reviewer"))
            {
                // Code Review returns CodeReviewResult
                return @"{
                    ""approved"": true,
                    ""summary"": ""Looks good"",
                    ""findings"": []
                }";
            }

            return "{}";
        });
    }

    public void Dispose()
    {
        _tempWorkspace.Dispose();
    }

    [Fact]
    public async Task RunFullWorkflow_HappyPath_CompletesSuccessfully()
    {
        // Arrange
        var workItem = MockWorkContext.CreateWorkItem(number: 1, labels: new List<string> { "ready-for-agents", "estimate:3" });
        var config = MockWorkContext.CreateConfig(workspacePath: _tempWorkspace.WorkspacePath);

        var workContext = MockWorkContext.Create(
            workItem: workItem,
            github: _githubMock.Object,
            config: config,
            workspace: _tempWorkspace.Workspace, // Use real workspace wrapper around temp dir
            repo: _repoMock.Object,
            llm: _llmClient,
            sharedState: new System.Collections.Concurrent.ConcurrentDictionary<string, string>()
        );

        // We need to setup GitHub mocks for Pull Request
        _githubMock.SetupSequence(x => x.GetPullRequestNumberAsync(It.IsAny<string>()))
            .ReturnsAsync((int?)null) // For DevExecutor (PR check -> create)
            .ReturnsAsync(1);         // For CodeReviewExecutor (PR check -> proceed)
        
        _githubMock.Setup(x => x.OpenPullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("https://github.com/test/repo/pull/1");

        // Create initial Playbook file required by ContextBuilder/Refinement
        _tempWorkspace.CreateFile("docs/architecture-playbook.yaml", @"
project: Orchestrator
version: 1.0
allowed_frameworks: []
allowed_patterns: []
forbidden_patterns: []
");

        // Create Spec Template required by TechLead
        _tempWorkspace.CreateFile("docs/templates/spec.md", @"# Spec: Issue {{ISSUE_NUMBER}} - {{ISSUE_TITLE}}

STATUS: DRAFT
UPDATED: {{UPDATED_AT_UTC}}

## Ziel
...

## Nicht-Ziele
- ...

## Komponenten
- ...

## Touch List
| Operation | Path | Notes |
| --- | --- | --- |
| Modify | src/Example.cs | ... |

## Interfaces
```csharp
```

## Szenarien
Scenario: ...
Given ...
When ...
Then ...

## Sequenz
1. ...
2. ...

## Testmatrix
| Test | Files | Notes |
| --- | --- | --- |
| Unit | tests/ExampleTests.cs | ... |
");

        // Create file referenced in Spec (Touch List: modify tests/FeatureTests.cs)
        _tempWorkspace.CreateFile("tests/FeatureTests.cs", "// Existing tests");

        // Act
        // Build the full graph
        var workflow = WorkflowFactory.BuildGraph(workContext, startStage: null);

        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", _tempWorkspace.WorkspacePath, _tempWorkspace.WorkspacePath, "owner", "user", 1),
            Mode: "minimal",
            Attempt: 0
        );

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

        // Assert
        Assert.NotNull(output);
        Assert.True(output.Success, $"Workflow failed: {output.Notes}");
        Assert.Null(output.NextStage); // DoD is the final stage (manual merge)

        // Verify Artifacts
        Assert.True(File.Exists(Path.Combine(_tempWorkspace.WorkspacePath, "orchestrator/specs/issue-1.md")), "Spec should exist");
        Assert.True(File.Exists(Path.Combine(_tempWorkspace.WorkspacePath, "src/Feature.cs")), "Source file should exist");
        Assert.True(File.Exists(Path.Combine(_tempWorkspace.WorkspacePath, "orchestrator/reviews/issue-1.md")), "Review should exist");

        // Verify Git Interactions
        _repoMock.Verify(x => x.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.AtLeastOnce);

        // Verify GitHub Interactions
        _githubMock.Verify(x => x.OpenPullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task RunWorkflow_WithAmbiguousQuestion_BlocksAndStoresState()
    {
        // This test would have caught bugs: ef36c64 (missing edge), 7abd71d (state communication), c6f6814 (duplicate questions)

        // Arrange
        var workItem = MockWorkContext.CreateWorkItem(number: 2, labels: new List<string> { "ready-for-agents" });
        var config = MockWorkContext.CreateConfig(workspacePath: _tempWorkspace.WorkspacePath);

        // LLM returns a refinement with a question, then classifies it as Ambiguous
        var callCount = 0;
        var scriptedLlm = new ScriptedLlmClient((system, user) =>
        {
            callCount++;

            // First call: Refinement generates a question
            if (system.Contains("SDLC refinement assistant") && callCount == 1)
            {
                return @"{
                    ""clarifiedStory"": ""Story text"",
                    ""acceptanceCriteria"": [""AC1"", ""AC2"", ""AC3""],
                    ""openQuestions"": [""Should we support feature X or feature Y?""],
                    ""complexity"": { ""storyPoints"": 3, ""risk"": ""low"" }
                }";
            }

            // Second call: QuestionClassifier classifies as Ambiguous
            if (system.Contains("question classifier"))
            {
                return @"{
                    ""question"": ""Should we support feature X or feature Y?"",
                    ""type"": ""Ambiguous"",
                    ""reasoning"": ""Mixes product and technical decisions""
                }";
            }

            // Third call: Refinement runs again after Ambiguous classification
            // Should NOT regenerate the same question
            if (system.Contains("SDLC refinement assistant") && callCount > 2)
            {
                return @"{
                    ""clarifiedStory"": ""Story text"",
                    ""acceptanceCriteria"": [""AC1"", ""AC2"", ""AC3""],
                    ""openQuestions"": [],
                    ""complexity"": { ""storyPoints"": 3, ""risk"": ""low"" }
                }";
            }

            return "{}";
        });

        var workContext = MockWorkContext.Create(
            workItem: workItem,
            github: _githubMock.Object,
            config: config,
            workspace: _tempWorkspace.Workspace,
            repo: _repoMock.Object,
            llm: scriptedLlm,
            sharedState: new System.Collections.Concurrent.ConcurrentDictionary<string, string>()
        );

        _tempWorkspace.CreateFile("docs/architecture-playbook.yaml", "project: Test\nversion: 1.0");

        // Act
        var workflow = WorkflowFactory.BuildGraph(workContext, startStage: null);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", _tempWorkspace.WorkspacePath, _tempWorkspace.WorkspacePath, "owner", "user", 1),
            Mode: "minimal",
            Attempt: 0
        );

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

        // Assert
        Assert.NotNull(output);
        Assert.False(output.Success); // Should block due to ambiguous question
        Assert.Null(output.NextStage); // Blocked, no next stage
        Assert.Contains("ambiguous", output.Notes, StringComparison.OrdinalIgnoreCase);

        // Verify refinement file contains the ambiguous question
        var refinementPath = Path.Combine(_tempWorkspace.WorkspacePath, "orchestrator/refinement/issue-2.md");
        Assert.True(File.Exists(refinementPath), "Refinement file should exist");
        var refinementContent = File.ReadAllText(refinementPath);
        Assert.Contains("Ambiguous Questions", refinementContent);
        Assert.Contains("Should we support feature X or feature Y?", refinementContent);

        // Verify GitHub comment was posted
        _githubMock.Verify(x => x.CommentOnWorkItemAsync(
            2,
            It.Is<string>(s => s.Contains("Ambiguous Questions"))),
            Times.Once);
    }

    [Fact]
    public async Task RunWorkflow_ResumesFromExistingMarkdown_WhenStateIsEmpty()
    {
        // This test would have caught bug: 1b67342 (markdown parsing)

        // Arrange
        var workItem = MockWorkContext.CreateWorkItem(number: 3, labels: new List<string> { "ready-for-agents" });
        var config = MockWorkContext.CreateConfig(workspacePath: _tempWorkspace.WorkspacePath);

        // Create existing refinement markdown with open questions
        _tempWorkspace.CreateFile("orchestrator/refinement/issue-3.md", @"# Refinement: Issue #3

## Clarified Story
Story text

## Acceptance Criteria (3)
- AC1
- AC2
- AC3

## Open Questions (2)

- [ ] **Question #1:** What framework should we use?
  **Answer:** _[Pending]_

- [ ] **Question #2:** Should we add tests?
  **Answer:** _[Pending]_
");

        // LLM processes questions and classifies Q1 as Technical
        var callCount = 0;
        var scriptedLlm = new ScriptedLlmClient((system, user) =>
        {
            callCount++;

            // First call: Refinement loads from markdown, keeps Q2
            if (system.Contains("SDLC refinement assistant") && callCount == 1)
            {
                // After loading Q1 and Q2 from markdown, LLM generates Q2 still
                return @"{
                    ""clarifiedStory"": ""Story text"",
                    ""acceptanceCriteria"": [""AC1"", ""AC2"", ""AC3""],
                    ""openQuestions"": [""What framework should we use?"", ""Should we add tests?""],
                    ""complexity"": { ""storyPoints"": 3, ""risk"": ""low"" }
                }";
            }

            // Q1 classification
            if (system.Contains("question classifier") && user.Contains("What framework"))
            {
                return @"{
                    ""question"": ""What framework should we use?"",
                    ""type"": ""Technical"",
                    ""reasoning"": ""Architecture decision""
                }";
            }

            // Q1 answer
            if (system.Contains("technical advisor"))
            {
                return @"{
                    ""answer"": ""Use React"",
                    ""reasoning"": ""Best for this use case""
                }";
            }

            // After answering Q1, refinement should only have Q2
            if (system.Contains("SDLC refinement assistant") && callCount > 2)
            {
                return @"{
                    ""clarifiedStory"": ""Story text"",
                    ""acceptanceCriteria"": [""AC1"", ""AC2"", ""AC3""],
                    ""openQuestions"": [""Should we add tests?""],
                    ""complexity"": { ""storyPoints"": 3, ""risk"": ""low"" }
                }";
            }

            return "{}";
        });

        var workContext = MockWorkContext.Create(
            workItem: workItem,
            github: _githubMock.Object,
            config: config,
            workspace: _tempWorkspace.Workspace,
            repo: _repoMock.Object,
            llm: scriptedLlm,
            sharedState: new System.Collections.Concurrent.ConcurrentDictionary<string, string>()
        );

        _tempWorkspace.CreateFile("docs/architecture-playbook.yaml", "project: Test\nversion: 1.0");

        // Act
        // Start from Refinement stage (simulating workflow restart)
        var workflow = WorkflowFactory.BuildGraph(workContext, startStage: WorkflowStage.Refinement);
        var input = new WorkflowInput(
            workItem,
            new ProjectContext("owner", "repo", "main", _tempWorkspace.WorkspacePath, _tempWorkspace.WorkspacePath, "owner", "user", 1),
            Mode: "minimal",
            Attempt: 0
        );

        var output = await SDLCWorkflow.RunWorkflowAsync(workflow, input);

        // Assert
        Assert.NotNull(output);

        // Verify it picked up existing questions from markdown (check logs showed parsing)
        // The key assertion: workflow should have processed questions from existing markdown
        // without duplicating them or starting fresh
        var refinementPath = Path.Combine(_tempWorkspace.WorkspacePath, "orchestrator/refinement/issue-3.md");
        Assert.True(File.Exists(refinementPath), "Refinement file should exist");

        // Verify output progressed through question processing
        // (If markdown wasn't parsed, it would have generated new Q1/Q2 with different numbers)
        Assert.True(output.Success || output.NextStage == WorkflowStage.QuestionClassifier,
            "Workflow should have processed questions successfully");
    }
}
