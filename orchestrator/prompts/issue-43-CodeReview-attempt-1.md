# System Prompt

You are an AI code reviewer. Focus on correctness, spec compliance, security, and tests. Return JSON only.

# User Prompt

Issue: Remove unused CreateBranchAsync/DeleteBranchAsync from IGitHubClient (#43)

Changed Files:

Diff Summary:


Return JSON with fields:
{
  "approved": boolean,
  "summary": string,
  "findings": [
    { "severity": "BLOCKER|MAJOR|MINOR", "category": string, "message": string, "file": string?, "line": number? }
  ]
}
