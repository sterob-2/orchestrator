{
  "question": "Is IGitHubClient part of a public API or distributed NuGet package consumed by external projects where removing these methods would be a breaking change?",
  "type": "Product",
  "reasoning": "The question is about the API surface and release/compatibility impact on external consumers — i.e., whether this is a public contract and thus a breaking-change decision. This concerns project scope, stakeholder impact and requirements, not an implementation detail."
}