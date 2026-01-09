namespace Orchestrator.App.Workflows;

internal static class WorkflowStateKeys
{
    public const string RefinementResult = "refinement_result";
    public const string DorGateResult = "dor_result";
    public const string QuestionClassificationResult = "question_classification_result";
    public const string ProductOwnerResult = "product_owner_result";
    public const string TechnicalAdvisorResult = "technical_advisor_result";
    public const string SpecGateResult = "specgate_result";
    public const string TechLeadResult = "techlead_result";
    public const string DevResult = "dev_result";
    public const string CodeReviewResult = "codereview_result";
    public const string DodGateResult = "dod_result";
    public const string ReleaseResult = "release_result";

    // Question tracking state
    public const string LastProcessedQuestion = "last_processed_question";
    public const string QuestionAttemptCount = "question_attempt_count";
    public const string CurrentQuestionAnswer = "current_question_answer";
}
