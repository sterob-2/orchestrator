using Microsoft.Agents.AI.Workflows;
using Moq;
using Orchestrator.App.Core.Interfaces;
using Orchestrator.App.Tests.TestHelpers;
using Orchestrator.App.Workflows;

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
        _githubMock.Setup(x => x.GetPullRequestNumberAsync(It.IsAny<string>()))
            .ReturnsAsync((int?)null); // PR doesn't exist yet
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
        var workflow = WorkflowFactory.BuildGraph(workContext, startOverride: null);
        
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
        Assert.Null(output.NextStage); // Release is the final stage

        // Verify Artifacts
        Assert.True(File.Exists(Path.Combine(_tempWorkspace.WorkspacePath, "orchestrator/specs/issue-1.md")), "Spec should exist");
        Assert.True(File.Exists(Path.Combine(_tempWorkspace.WorkspacePath, "src/Feature.cs")), "Source file should exist");
        Assert.True(File.Exists(Path.Combine(_tempWorkspace.WorkspacePath, "orchestrator/reviews/issue-1.md")), "Review should exist");
        Assert.True(File.Exists(Path.Combine(_tempWorkspace.WorkspacePath, "orchestrator/release/issue-1.md")), "Release notes should exist");

        // Verify Git Interactions
        _repoMock.Verify(x => x.CommitAndPush(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.AtLeastOnce);
        
        // Verify GitHub Interactions
        _githubMock.Verify(x => x.OpenPullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
