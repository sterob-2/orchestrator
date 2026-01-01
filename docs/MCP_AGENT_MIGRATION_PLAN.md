# MCP Agent Migration Plan

## Current Status

### ✅ Phase 1: MCP Infrastructure (COMPLETED)

**Accomplished:**
- Integrated ModelContextProtocol NuGet package (v0.5.0-preview.1)
- Created McpClientManager for managing MCP server connections
- Configured Docker-in-Docker pattern for spawning MCP containers
- Successfully connected to Filesystem and GitHub MCP servers
- **Total: 54 MCP tools available** (14 filesystem + 40 GitHub)

**Files Modified:**
- `src/Orchestrator.App/Orchestrator.App.csproj` - Added MCP packages
- `src/Orchestrator.App/McpClientManager.cs` - NEW: MCP client infrastructure
- `src/Orchestrator.App/OrchestratorConfig.cs` - Added WorkspaceHostPath configuration
- `src/Orchestrator.App/Models.cs` - Added Mcp to WorkContext
- `src/Orchestrator.App/Program.cs` - Initialize and pass MCP manager
- `Dockerfile` - Added Docker CLI and git configuration
- `docker-compose.yml` - Added Docker socket mount and workspace configuration
- `.env` - Added WORKSPACE_HOST_PATH configuration

### ⏭️ Phase 2: Agent Migration (NEXT)

**Goal:** Migrate agents from custom tool implementations to use MCP tools via Microsoft.Extensions.AI

**Current Agent Architecture:**
```
Agent (IRoleAgent)
  → Uses LlmClient (raw OpenAI SDK)
  → Uses ctx.Workspace (custom RepoWorkspace class)
  → Uses ctx.Repo (custom RepoGit class using LibGit2Sharp)
  → Uses ctx.GitHub (custom OctokitGitHubClient)
```

**Target Agent Architecture:**
```
Agent (Agent Framework AgentClient)
  → Uses Microsoft.Extensions.AI ChatClient
  → Uses MCP tools via tool calling
  → LLM autonomously calls tools as needed
```

## Migration Approaches

### Option A: Incremental Migration (Recommended)

Gradually migrate to MCP tools while maintaining backward compatibility.

**Steps:**

1. **Update LlmClient to support tool calling** (1-2 days)
   - Replace raw OpenAI SDK with Microsoft.Extensions.AI ChatClient
   - Add support for passing MCP tools to chat completion
   - Implement tool call handling and result processing
   - Keep existing GetUpdatedFileAsync method for compatibility

2. **Create tool-enabled agent base class** (1 day)
   - New `ToolEnabledAgentBase` class that extends IRoleAgent
   - Automatically provides MCP tools to LLM
   - Handles tool call loop (call → execute → return result → continue)
   - Agents can opt-in to use tools

3. **Migrate agents one by one** (2-3 days per agent)
   - Start with DevAgent (most file operations)
   - Replace direct ctx.Workspace calls with LLM tool calls
   - Update prompts to guide LLM to use tools
   - Add integration tests for each migrated agent
   - Order: DevAgent → TechLeadAgent → TestAgent → ReleaseAgent → PlannerAgent

4. **Deprecate custom tool classes gradually**
   - Keep RepoWorkspace, RepoGit, OctokitGitHubClient for backward compatibility
   - Mark as [Obsolete] once all agents migrated
   - Remove in Phase 3

**Pros:**
- Low risk - can roll back at any point
- Agents can be tested individually
- No "big bang" deployment
- Learn and adjust approach as we go

**Cons:**
- Takes longer
- Temporary code duplication
- More commits/PRs

### Option B: Full Framework Migration (Ambitious)

Complete rewrite to use Microsoft Agent Framework's AgentClient and workflow system.

**Steps:**

1. **Replace IRoleAgent with Agent Framework's AgentClient** (2-3 days)
   - Define agents using Agent Framework's agent definition
   - Use Agent Framework's built-in tool calling mechanism
   - Migrate all agents to new architecture simultaneously

2. **Implement Agent Framework workflows** (2-3 days)
   - Replace current state machine (stage labels) with Agent Framework workflows
   - Define agent transitions and conditions
   - Implement workflow persistence

3. **Update all agents to new architecture** (3-5 days)
   - Rewrite agent logic for new framework
   - Update prompts for tool-based approach
   - Comprehensive testing

4. **Remove all custom tool implementations**
   - Delete RepoWorkspace, RepoGit, OctokitGitHubClient
   - Simplify WorkContext

**Pros:**
- Fully aligned with framework best practices
- Cleaner architecture
- Better tool calling support
- Potentially more powerful (agentic workflows)

**Cons:**
- High risk - all or nothing
- Longer development time
- More complex testing
- Harder to debug issues

### Option C: Hybrid Approach (Pragmatic)

Migrate to Microsoft.Extensions.AI ChatClient with tool calling, but keep current agent architecture.

**Steps:**

1. **Update LlmClient to use Microsoft.Extensions.AI** (1 day)
   - Replace OpenAI SDK with ChatClient
   - Add overload for tool-enabled chat completion
   - Keep current method signatures for compatibility

2. **Add MCP tool support to specific agents** (2-3 days)
   - DevAgent: Use filesystem tools for file operations
   - ReleaseAgent: Use GitHub tools for PR creation
   - Keep other agents using direct methods

3. **Gradually increase tool usage** (ongoing)
   - Monitor which operations work well with tools
   - Expand tool usage based on results
   - Keep direct methods as fallback

**Pros:**
- Lowest risk
- Can start seeing benefits quickly
- Agents remain testable with current tests
- Incremental improvement

**Cons:**
- Not fully leveraging framework
- Still have custom tool implementations
- Mixed architecture (technical debt)

## Recommended Approach: Option A (Incremental Migration)

**Rationale:**
1. **De-risks the migration** - we can validate the approach with one agent before committing
2. **Allows learning** - MCP tool calling behavior may be different than expected
3. **Maintains stability** - production can continue on old architecture while we migrate
4. **Testable** - each agent can be tested thoroughly before deployment
5. **Reversible** - if MCP tools don't work as expected, we haven't lost our working implementation

## Implementation Plan for Option A

### Phase 2.1: LlmClient Modernization (Week 1)

**Goal:** Update LlmClient to support tool calling while maintaining backward compatibility

**Tasks:**
1. Add Microsoft.Extensions.AI.OpenAI NuGet package
2. Create new `ChatClient` instance in LlmClient
3. Add new method: `Task<ChatCompletion> CompleteChatWithToolsAsync(messages, tools, options)`
4. Implement tool call handling loop:
   ```
   while (completion.HasToolCalls):
       results = ExecuteToolCalls(completion.ToolCalls)
       messages.Add(results)
       completion = await chatClient.CompleteAsync(messages, tools)
   ```
5. Unit tests for tool calling
6. Integration test with real MCP tools

**Files to modify:**
- `src/Orchestrator.App/LlmClient.cs`
- `tests/Orchestrator.App.Tests/LlmClientTests.cs` (new)

### Phase 2.2: Tool-Enabled Agent Base (Week 1-2)

**Goal:** Create infrastructure for agents to use MCP tools

**Tasks:**
1. Create `ToolEnabledAgentBase` abstract class
2. Add `GetRequiredTools()` virtual method (agents specify which tools they need)
3. Add `BuildSystemPromptWithTools()` method (instructs LLM on tool usage)
4. Add `RunWithToolsAsync()` method (handles tool call loop)
5. Update WorkContext to provide easy access to MCP tools by server
6. Integration tests

**Files to create:**
- `src/Orchestrator.App/Agents/ToolEnabledAgentBase.cs`
- `tests/Orchestrator.App.Tests/Agents/ToolEnabledAgentBaseTests.cs`

**Files to modify:**
- `src/Orchestrator.App/Models.cs` (add helper methods to WorkContext)

### Phase 2.3: Migrate DevAgent (Week 2-3)

**Goal:** First production agent using MCP tools

**Tasks:**
1. Make DevAgent inherit from ToolEnabledAgentBase
2. Update system prompt to guide tool usage
3. Replace direct file operations:
   ```csharp
   // Before
   var content = ctx.Workspace.ReadAllText(file);

   // After
   // LLM decides to call read_file tool
   // Framework executes tool
   // LLM gets file content in tool result
   ```
4. Add validation that LLM used correct tools
5. Integration tests comparing old vs new behavior
6. Test on real work items
7. Monitor for quality/correctness

**Files to modify:**
- `src/Orchestrator.App/Agents/DevAgent.cs`
- `tests/Orchestrator.App.Tests/Agents/DevAgentTests.cs`

### Phase 2.4: Migrate Remaining Agents (Week 4-6)

**Agents in order:**
1. TechLeadAgent (file reading, spec validation)
2. TestAgent (file reading, test execution)
3. ReleaseAgent (GitHub operations, PR creation)
4. PlannerAgent (file reading, planning)

**Each agent follows same pattern:**
1. Inherit from ToolEnabledAgentBase
2. Update prompts
3. Remove direct tool calls
4. Add tests
5. Deploy and monitor

### Phase 2.5: Deprecate Custom Tools (Week 7)

**Tasks:**
1. Mark RepoWorkspace as [Obsolete]
2. Mark direct RepoGit methods as [Obsolete]
3. Mark OctokitGitHubClient as [Obsolete]
4. Update documentation
5. Plan for removal in Phase 3

## Success Criteria

**For each migrated agent:**
- ✅ Agent successfully completes work items using MCP tools
- ✅ No increase in error rate compared to old implementation
- ✅ LLM consistently uses correct tools
- ✅ Tool calls complete within acceptable time
- ✅ Integration tests pass
- ✅ Code review approved

**For overall migration:**
- ✅ All agents using MCP tools
- ✅ Custom tool implementations deprecated
- ✅ Documentation updated
- ✅ Performance metrics acceptable
- ✅ Zero critical bugs introduced

## Open Questions

1. **Tool call reliability:** How reliably will LLMs use the correct tools? May need prompt engineering.

2. **Error handling:** How to handle tool call failures gracefully? May need retry logic.

3. **Performance:** Will tool calling add significant latency? May need optimization.

4. **Observability:** How to debug tool call chains? May need enhanced logging.

5. **Git MCP server:** Why is it failing? Needs investigation before Phase 2.

## Risk Mitigation

1. **Feature flags:** Add `USE_MCP_TOOLS` flag to .env to toggle between old/new
2. **Metrics:** Track success rate, latency, token usage for each approach
3. **Gradual rollout:** Deploy to staging first, monitor for 1 week
4. **Rollback plan:** Keep old implementation for quick rollback
5. **Comprehensive testing:** Unit + integration + end-to-end tests

## Timeline Estimate (Option A)

- **Week 1:** LlmClient modernization + ToolEnabledAgentBase
- **Week 2-3:** DevAgent migration + testing
- **Week 4:** TechLeadAgent + TestAgent migration
- **Week 5:** ReleaseAgent + PlannerAgent migration
- **Week 6:** Integration testing + fixes
- **Week 7:** Deprecation + documentation

**Total: 6-8 weeks for complete migration**

## Next Steps

1. ✅ Review this plan
2. ✅ Decide on migration approach (A, B, or C)
3. ✅ Create Phase 2.1 tasks in backlog
4. ✅ Set up feature branch for migration work
5. ✅ Begin LlmClient modernization

## Notes

- MCP tools are READ-ONLY for filesystem operations (`:ro` mount), which may limit some use cases
- Git MCP server needs debugging before git operations can be migrated
- GitHub token needs to be available for GitHub MCP tools to work
- Tool calls may increase token usage - monitor costs
