namespace Orchestrator.App.Workflows;

internal enum WorkflowStage
{
    ContextBuilder,
    Refinement,
    QuestionClassifier,
    ProductOwner,
    TechnicalAdvisor,
    DoR,
    TechLead,
    SpecGate,
    Dev,
    CodeReview,
    DoD,
    Release
}
