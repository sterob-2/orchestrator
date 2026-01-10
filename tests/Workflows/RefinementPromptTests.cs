using Orchestrator.App.Core.Models;
using Orchestrator.App.Parsing;
using Orchestrator.App.Workflows;

namespace Orchestrator.App.Tests.Workflows;

public class RefinementPromptTests
{
    [Fact]
    public void Build_WithBasicWorkItem_ReturnsPrompt()
    {
        var workItem = new WorkItem(1, "Test Title", "Test Body", "url", new List<string>());
        var playbook = new Playbook();

        var (system, user) = RefinementPrompt.Build(workItem, playbook, null);

        Assert.Contains("SDLC refinement assistant", system);
        Assert.Contains("Test Title", user);
        Assert.Contains("Test Body", user);
        Assert.Contains("Return JSON", user);
        Assert.Contains("Acceptance Criteria Requirements", user);
    }

    [Fact]
    public void Build_IncludesBddFormatRequirements()
    {
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var playbook = new Playbook();

        var (system, user) = RefinementPrompt.Build(workItem, playbook, null);

        // System prompt should emphasize testable criteria
        Assert.Contains("CRITICAL", system);
        Assert.Contains("testable", system, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("BDD format", system);

        // User prompt should have detailed requirements
        Assert.Contains("IMPORTANT - Acceptance Criteria Requirements", user);
        Assert.Contains("MUST write at least 3 testable acceptance criteria", user);
        Assert.Contains("BDD format:", user);
        Assert.Contains("Given [context], when [action], then [outcome]", user);
        Assert.Contains("'should', 'must', 'verify', 'ensure'", user);

        // Should have examples
        Assert.Contains("Examples of VALID acceptance criteria", user);
        Assert.Contains("Examples of INVALID acceptance criteria", user);
        Assert.Contains("Given a user is logged in", user);
        Assert.Contains("not testable", user);
    }

    [Fact]
    public void Build_WithPreviousRefinement_IncludesPreviousRefinement()
    {
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var playbook = new Playbook();
        var previousRefinement = "# Previous refinement\n## Open Questions\n- Question 1?";

        var (_, user) = RefinementPrompt.Build(workItem, playbook, null, previousRefinement);

        Assert.Contains("Previous Refinement", user);
        Assert.Contains("Question 1?", user);
    }

    [Fact]
    public void Build_WithAnsweredQuestions_IncludesAnswersWithCheckboxes()
    {
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var playbook = new Playbook();
        var answeredQuestions = new List<AnsweredQuestion>
        {
            new AnsweredQuestion(1, "What is the target user?", "Enterprise users", "ProductOwner"),
            new AnsweredQuestion(2, "Which database to use?", "PostgreSQL 15", "TechnicalAdvisor")
        };

        var (system, user) = RefinementPrompt.Build(workItem, playbook, null, null, answeredQuestions);

        // System prompt should remind not to re-ask answered questions
        Assert.Contains("Do NOT re-ask answered questions", system);

        // User prompt should show answered questions with checkboxes
        Assert.Contains("Previously Answered Questions", user);
        Assert.Contains("- [x] **Question #1:** What is the target user?", user);
        Assert.Contains("**Answer (ProductOwner):** Enterprise users", user);
        Assert.Contains("- [x] **Question #2:** Which database to use?", user);
        Assert.Contains("**Answer (TechnicalAdvisor):** PostgreSQL 15", user);
    }

    [Fact]
    public void Build_WithPlaybook_IncludesPlaybookConstraints()
    {
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var playbook = new Playbook
        {
            AllowedFrameworks = new List<FrameworkDef>
            {
                new FrameworkDef { Name = "React", Id = "react", Version = "18" }
            },
            AllowedPatterns = new List<PatternDef>
            {
                new PatternDef { Name = "MVC", Id = "mvc", Reference = "Model-View-Controller" }
            }
        };

        var (_, user) = RefinementPrompt.Build(workItem, playbook, null);

        Assert.Contains("Playbook Constraints", user);
        Assert.Contains("React", user);
        Assert.Contains("MVC", user);
    }

    [Fact]
    public void Build_WithExistingSpec_IncludesSpec()
    {
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var playbook = new Playbook();
        var existingSpec = "# Existing Spec\n## Details\nSome details here";

        var (_, user) = RefinementPrompt.Build(workItem, playbook, existingSpec);

        Assert.Contains("Existing Spec", user);
        Assert.Contains("Some details here", user);
    }

    [Fact]
    public void Build_WithEmptyPlaybook_ShowsNone()
    {
        var workItem = new WorkItem(1, "Title", "Body", "url", new List<string>());
        var playbook = new Playbook();

        var (_, user) = RefinementPrompt.Build(workItem, playbook, null);

        Assert.Contains("Playbook Constraints:", user);
        Assert.Contains("None", user);
    }

    [Fact]
    public void Fallback_ParsesAcceptanceCriteriaFromBody()
    {
        var body = "Description\n\nAcceptance Criteria:\n- Given X\n- When Y\n- Then Z";
        var workItem = new WorkItem(1, "Title", body, "url", new List<string>());

        var result = RefinementPrompt.Fallback(workItem);

        Assert.Equal(body, result.ClarifiedStory);
        Assert.Equal(3, result.AcceptanceCriteria.Count);
        Assert.Contains("Given X", result.AcceptanceCriteria);
        Assert.Single(result.OpenQuestions);
        Assert.Contains("invalid", result.OpenQuestions[0].Question, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Fallback_WithEmptyBody_UsesTitleAsStory()
    {
        var workItem = new WorkItem(1, "Test Title", "", "url", new List<string>());

        var result = RefinementPrompt.Fallback(workItem);

        Assert.Equal("Test Title", result.ClarifiedStory);
        Assert.Empty(result.AcceptanceCriteria);
        Assert.Single(result.OpenQuestions);
    }
}
