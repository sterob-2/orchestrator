# Microsoft Agent Framework Workflow Prototype - Testing Guide

This guide walks you through testing the new Microsoft Agent Framework-based workflow implementation.

## What Was Built

We've created a **minimal working prototype** that implements the Planner stage using Microsoft Agent Framework:

### New Files Created

1. **`WorkflowExecutors.cs`** - Contains:
   - `WorkflowInput` - Input message type for workflows
   - `WorkflowOutput` - Output message type from executors
   - `PlannerExecutor` - Agent Framework executor that wraps PlannerAgent logic

2. **`SDLCWorkflow.cs`** - Contains:
   - `BuildPlannerOnlyWorkflow()` - Creates a simple workflow with just Planner
   - `RunWorkflowAsync()` - Executes workflow and handles events

3. **Updated Files**:
   - `Orchestrator.App.csproj` - Added `Microsoft.Agents.AI.Workflows` package
   - `OrchestratorConfig.cs` - Added `UseWorkflowMode` flag
   - `Program.cs` - Added `HandlePlannerWorkflowAsync()` method with conditional routing
   - `.env.example` - Added `USE_WORKFLOW_MODE` environment variable

## How It Works

### Architecture

```
GitHub Issue with "agent:planner" label
    ↓
Program.cs checks cfg.UseWorkflowMode flag
    ↓
├─ false → Old PlannerAgent (label-based)
└─ true  → New PlannerExecutor (workflow-based)
         ↓
    BuildPlannerOnlyWorkflow()
         ↓
    Creates Workflow with PlannerExecutor
         ↓
    Executes with checkpointing enabled
         ↓
    Returns WorkflowOutput
         ↓
    Updates labels: remove "agent:planner", add "agent:techlead"
```

### Key Features

- ✅ **Checkpointing** - Workflow state is saved automatically
- ✅ **Event Streaming** - Observe executor start, completion, checkpoints
- ✅ **State Persistence** - Executor state can be saved/restored (though Planner is stateless)
- ✅ **Error Handling** - Exceptions are caught and logged
- ✅ **Backward Compatible** - Old label-based system still works when flag is false

---

## Testing Steps

### 1. Prerequisites

Ensure you have:
- .NET 8 SDK installed
- Valid GitHub token with repo access
- OpenAI API key (or compatible endpoint)
- A GitHub repository with the orchestrator running

### 2. Enable Workflow Mode

Edit your `.env` file (or copy from `.env.example`):

```bash
# Enable workflow mode
USE_WORKFLOW_MODE=true

# Ensure other required vars are set
GITHUB_TOKEN=ghp_your_token_here
OPENAI_API_KEY=sk-your_key_here
REPO_OWNER=your-username
REPO_NAME=your-repo
DEFAULT_BASE_BRANCH=main
WORKSPACE_PATH=/path/to/your/repo
```

### 3. Restore Packages

```bash
cd orchestrator/src/Orchestrator.App
dotnet restore
```

This will download the new `Microsoft.Agents.AI.Workflows` package.

### 4. Build the Project

```bash
dotnet build
```

Check for any compilation errors. If you see errors related to missing types, ensure the packages were restored correctly.

### 5. Create a Test Issue

In your GitHub repository:

1. Create a new issue with:
   - **Title**: "Test Agent Framework Workflow"
   - **Body**:
     ```markdown
     This is a test issue for the new Microsoft Agent Framework workflow.

     ## Acceptance Criteria
     - [ ] Plan is created
     - [ ] Draft PR is opened
     - [ ] Workflow completes successfully
     ```
   - **Labels**: Add `agent:planner`

### 6. Run the Orchestrator

```bash
dotnet run
```

Watch the console output. You should see:

```
🔄 Workflow Mode: PlannerExecutor handling work item #123.
🚀 Starting workflow for issue #123...
▶️  Executor started: Planner
✅ Executor completed: Planner
💾 Checkpoint created at step 1
🎉 Workflow completed!
   Success: True
   Notes: Planner created branch `agent/issue-123`, opened a draft PR, and wrote `orchestrator/plans/issue-123.md`.
   Next Stage: TechLead
✅ Workflow completed successfully for issue #123
```

### 7. Verify Results

Check the following:

#### ✅ GitHub Issue
- Issue should have a comment from the orchestrator with success notes
- Label `agent:planner` should be removed
- Label `agent:techlead` should be added

#### ✅ Git Branch
```bash
git fetch origin
git branch -r | grep agent/issue-123
```
Should show: `origin/agent/issue-123`

#### ✅ Plan Artifact
```bash
cat orchestrator/plans/issue-123.md
```
Should contain:
- Issue metadata (number, title)
- Status: COMPLETE
- Acceptance criteria copied from issue body

#### ✅ Pull Request
- Check GitHub for a draft PR
- Title should be: "Agent Plan: Test Agent Framework Workflow"
- Body should reference the plan file

---

## Comparing Legacy vs Workflow Mode

### Legacy Mode (USE_WORKFLOW_MODE=false)

**Console Output:**
```
PlannerAgent handling work item #123.
```

**Behavior:**
- Uses old `PlannerAgent` class
- Direct method calls
- No checkpointing
- No event streaming

### Workflow Mode (USE_WORKFLOW_MODE=true)

**Console Output:**
```
🔄 Workflow Mode: PlannerExecutor handling work item #123.
🚀 Starting workflow for issue #123...
▶️  Executor started: Planner
✅ Executor completed: Planner
💾 Checkpoint created at step 1
🎉 Workflow completed!
```

**Behavior:**
- Uses new `PlannerExecutor` in workflow
- Graph-based execution
- Checkpointing enabled
- Event streaming with detailed logs
- State can be saved/restored

---

## Debugging

### Check Which Mode is Active

Add this to your `.env`:
```bash
USE_WORKFLOW_MODE=true
```

Then in the console, look for:
- `🔄 Workflow Mode:` → Using Agent Framework
- `PlannerAgent handling` → Using legacy mode

### View Workflow Events

The workflow emits these events (all logged to console):
- `ExecutorStartedEvent` - When executor begins processing
- `ExecutorCompletedEvent` - When executor finishes
- `SuperStepCompletedEvent` - After each workflow step (includes checkpoint)
- `WorkflowOutputEvent` - Final result
- `WorkflowFailedEvent` - If something goes wrong

### Common Issues

**Issue**: Package not found
```
error NU1102: Unable to find package 'Microsoft.Agents.AI.Workflows'
```
**Fix**: Run `dotnet restore --force` to refresh package cache.

**Issue**: Workflow doesn't run
- Check `.env` has `USE_WORKFLOW_MODE=true`
- Ensure issue has `agent:planner` label
- Check console for which mode is active

**Issue**: Compilation errors
```
error CS0246: The type or namespace name 'Workflow' could not be found
```
**Fix**:
1. Check `Orchestrator.App.csproj` includes the Workflows package
2. Add `using Microsoft.Agents.AI.Workflows;` at top of file

---

## What's Next?

Once the Planner workflow is tested and working:

### Phase 1: Expand Workflow Graph

Add TechLead and Dev executors:

```csharp
public static Workflow BuildFullWorkflow(WorkContext context)
{
    var plannerExecutor = new PlannerExecutor("Planner", context);
    var techLeadExecutor = new TechLeadExecutor("TechLead", context);
    var devExecutor = new DevExecutor("Dev", context);

    var workflow = new WorkflowBuilder()
        .AddEdge(plannerExecutor, techLeadExecutor)
        .AddEdge(techLeadExecutor, devExecutor)
        .SetStartExecutor(plannerExecutor)
        .WithCheckpointing(CheckpointManager.Default)
        .Build();

    return workflow;
}
```

### Phase 2: Add Tools to DevExecutor

Implement compilation and test tools:

```csharp
internal sealed class DevExecutor : Executor<SpecMessage, CodeResult>
{
    public override async ValueTask HandleAsync(...)
    {
        // Generate code
        var code = await GenerateCodeAsync(...);

        // Tool: Compile
        var compileResult = await CompileToolAsync(code);
        if (!compileResult.Success)
        {
            // Retry with error context
            code = await FixCodeAsync(code, compileResult.Errors);
        }

        // Tool: Run tests
        var testResult = await RunTestsToolAsync();
        if (!testResult.Success)
        {
            // Fix and retry
        }

        await context.SendMessageAsync(result);
    }
}
```

### Phase 3: Add Conditional Routing

Implement the code review loop:

```csharp
.AddSwitchCaseEdgeGroup(
    codeReviewExecutor,
    new[]
    {
        new Case<ReviewResult>(
            condition: r => r.Decision == "APPROVED",
            target: testExecutor
        ),
        new Case<ReviewResult>(
            condition: r => r.Decision == "CHANGES_REQUESTED",
            target: devExecutor  // Loop back
        )
    }
)
```

### Phase 4: Add Human-in-the-Loop

Implement request/response for spec questions:

```csharp
// In DevExecutor
if (spec.IsUnclear)
{
    var answer = await context.RequestAsync<Question, Answer>(
        new Question { Text = "Please clarify..." }
    );
    // Use answer to proceed
}
```

---

## Success Criteria

The prototype is successful if:

1. ✅ Issue with `agent:planner` label is processed
2. ✅ Console shows workflow events (started, completed, checkpoint)
3. ✅ Plan file is created in `orchestrator/plans/issue-N.md`
4. ✅ Git branch `agent/issue-N` is created
5. ✅ Draft PR is opened on GitHub
6. ✅ Issue labels updated (planner removed, techlead added)
7. ✅ Comment posted with workflow result
8. ✅ No errors in console output

---

## Feedback

After testing, document:
- What worked well?
- What broke or didn't work as expected?
- Performance compared to legacy mode?
- Any issues with checkpointing or event streaming?

This feedback will guide the next iteration as we expand to TechLead, Dev, Test, and Release executors.
