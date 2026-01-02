using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ModelContextProtocol.Client;

namespace Orchestrator.App.Agents;

/// <summary>
/// Base class for agents that use MCP tools for their operations.
/// Provides infrastructure for tool selection and LLM-based tool calling.
/// </summary>
/// <remarks>
/// This is the foundation for Phase 2 of MCP integration. Agents inheriting from this
/// class can specify which MCP tools they need and use them via LLM tool calling instead
/// of direct method invocations on WorkContext.
///
/// Benefits:
/// - LLM decides when and how to use tools autonomously
/// - More flexible than hard-coded operations
/// - Better handles edge cases and errors
/// - Reduced custom code in agents
///
/// See docs/MCP_AGENT_MIGRATION_PLAN.md for migration strategy.
/// </remarks>
internal abstract class ToolEnabledAgentBase : IRoleAgent
{
    /// <summary>
    /// Specifies which MCP tools this agent needs access to.
    /// Override to return the specific tools required by the agent.
    /// </summary>
    /// <param name="ctx">Work context containing all available MCP tools</param>
    /// <returns>List of MCP tools the agent will use</returns>
    protected virtual IEnumerable<McpClientTool> GetRequiredTools(WorkContext ctx)
    {
        // Default: no tools (backward compatible)
        return Enumerable.Empty<McpClientTool>();
    }

    /// <summary>
    /// Builds the system prompt with tool usage instructions.
    /// Override to customize how the agent is instructed to use tools.
    /// </summary>
    /// <param name="basePrompt">The agent's base system prompt</param>
    /// <param name="tools">Available MCP tools</param>
    /// <returns>Enhanced system prompt with tool instructions</returns>
    protected virtual string BuildSystemPromptWithTools(string basePrompt, IEnumerable<McpClientTool> tools)
    {
        var toolsList = tools.ToList();
        if (toolsList.Count == 0)
        {
            return basePrompt;
        }

        var toolDescriptions = string.Join("\n", toolsList.Select(t =>
            $"- {t.Name}: {t.Description}"));

        return $@"{basePrompt}

## Available Tools

You have access to the following tools to complete your task:

{toolDescriptions}

Use these tools when needed to accomplish your goals. Call tools by their exact names with appropriate arguments.";
    }

    /// <summary>
    /// Runs the agent with tool calling support.
    /// This method handles the tool call loop automatically.
    /// </summary>
    /// <param name="ctx">Work context</param>
    /// <param name="systemPrompt">System prompt for the agent</param>
    /// <param name="userPrompt">User prompt describing the task</param>
    /// <param name="model">LLM model to use</param>
    /// <returns>Final response from LLM after all tool calls complete</returns>
    protected async Task<string> RunWithToolsAsync(
        WorkContext ctx,
        string systemPrompt,
        string userPrompt,
        string model)
    {
        if (ctx.Mcp == null)
        {
            // No MCP available, fall back to standard completion
            Logger.WriteLine("[Agent] No MCP available, using standard LLM completion");
            return await ctx.Llm.GetUpdatedFileAsync(model, systemPrompt, userPrompt);
        }

        var tools = GetRequiredTools(ctx).ToList();
        if (tools.Count == 0)
        {
            // Agent doesn't use tools, use standard completion
            return await ctx.Llm.GetUpdatedFileAsync(model, systemPrompt, userPrompt);
        }

        var enhancedPrompt = BuildSystemPromptWithTools(systemPrompt, tools);

        Logger.WriteLine($"[Agent] Running with {tools.Count} MCP tools available");

        // Use tool-enabled completion
        return await ctx.Llm.CompleteChatWithMcpToolsAsync(
            model,
            enhancedPrompt,
            userPrompt,
            tools);
    }

    /// <summary>
    /// Main entry point for the agent. Must be implemented by derived classes.
    /// </summary>
    public abstract Task<AgentResult> RunAsync(WorkContext ctx);
}

/// <summary>
/// Helper class for filtering MCP tools by server name.
/// </summary>
internal static class McpToolFilters
{
    /// <summary>
    /// Gets all filesystem tools from the MCP manager.
    /// </summary>
    public static IEnumerable<McpClientTool> FilesystemTools(this McpClientManager mcp)
    {
        return mcp.GetToolsByServer("filesystem");
    }

    /// <summary>
    /// Gets all git tools from the MCP manager.
    /// </summary>
    public static IEnumerable<McpClientTool> GitTools(this McpClientManager mcp)
    {
        return mcp.GetToolsByServer("git");
    }

    /// <summary>
    /// Gets all GitHub tools from the MCP manager.
    /// </summary>
    public static IEnumerable<McpClientTool> GitHubTools(this McpClientManager mcp)
    {
        return mcp.GetToolsByServer("github");
    }

    /// <summary>
    /// Gets specific tools by name from any server.
    /// </summary>
    public static IEnumerable<McpClientTool> ToolsByName(this McpClientManager mcp, params string[] toolNames)
    {
        var nameSet = new HashSet<string>(toolNames, StringComparer.OrdinalIgnoreCase);
        return mcp.Tools.Where(t => nameSet.Contains(t.Name));
    }
}
