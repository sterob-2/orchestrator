namespace Orchestrator.App.Workflows;

internal static class WorkflowStageGraph
{
    public static WorkflowStage StartStageFromLabels(LabelConfig labels, WorkItem item)
    {
        if (HasLabel(item, labels.WorkItemLabel) || HasLabel(item, labels.PlannerLabel))
        {
            return WorkflowStage.Refinement;
        }

        if (HasLabel(item, labels.DorLabel))
        {
            return WorkflowStage.DoR;
        }

        if (HasLabel(item, labels.TechLeadLabel))
        {
            return WorkflowStage.TechLead;
        }

        if (HasLabel(item, labels.SpecGateLabel))
        {
            return WorkflowStage.SpecGate;
        }

        if (HasLabel(item, labels.DevLabel))
        {
            return WorkflowStage.Dev;
        }

        if (HasLabel(item, labels.TestLabel))
        {
            return WorkflowStage.DoD;
        }

        if (HasLabel(item, labels.CodeReviewNeededLabel) || HasLabel(item, labels.CodeReviewChangesRequestedLabel))
        {
            return WorkflowStage.CodeReview;
        }

        return WorkflowStage.Refinement;
    }

    public static WorkflowStage? NextStageFor(WorkflowStage stage)
    {
        return NextStageFor(stage, success: true);
    }

    public static WorkflowStage? NextStageFor(WorkflowStage stage, bool success)
    {
        return stage switch
        {
            WorkflowStage.ContextBuilder => WorkflowStage.Refinement,
            WorkflowStage.Refinement => WorkflowStage.DoR,
            WorkflowStage.QuestionClassifier => null, // Routes to TechnicalAdvisor or ProductOwner based on classification
            WorkflowStage.ProductOwner => WorkflowStage.Refinement, // Returns answer to Refinement
            WorkflowStage.TechnicalAdvisor => WorkflowStage.Refinement, // Returns answer to Refinement
            WorkflowStage.DoR => success ? WorkflowStage.TechLead : WorkflowStage.Refinement,
            WorkflowStage.TechLead => WorkflowStage.SpecGate,
            WorkflowStage.SpecGate => success ? WorkflowStage.Dev : WorkflowStage.TechLead,
            WorkflowStage.Dev => WorkflowStage.CodeReview,
            WorkflowStage.CodeReview => success ? WorkflowStage.DoD : WorkflowStage.Dev,
            WorkflowStage.DoD => success ? null : WorkflowStage.Dev,
            _ => null
        };
    }

    public static string ExecutorIdFor(WorkflowStage stage)
    {
        return stage switch
        {
            WorkflowStage.ContextBuilder => "ContextBuilder",
            WorkflowStage.Refinement => "Refinement",
            WorkflowStage.QuestionClassifier => "QuestionClassifier",
            WorkflowStage.ProductOwner => "ProductOwner",
            WorkflowStage.TechnicalAdvisor => "TechnicalAdvisor",
            WorkflowStage.DoR => "DoR",
            WorkflowStage.TechLead => "TechLead",
            WorkflowStage.SpecGate => "SpecGate",
            WorkflowStage.Dev => "Dev",
            WorkflowStage.CodeReview => "CodeReview",
            WorkflowStage.DoD => "DoD",
            _ => "Refinement"
        };
    }

    private static bool HasLabel(WorkItem item, string label)
    {
        return item.Labels.Contains(label, StringComparer.OrdinalIgnoreCase);
    }
}
