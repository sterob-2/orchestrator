using System.Text.RegularExpressions;
using Orchestrator.App.Core.Models;

namespace Orchestrator.App.Parsing;

/// <summary>
/// Parser for extracting answered questions from refinement markdown files
/// </summary>
internal static partial class RefinementMarkdownParser
{
    [GeneratedRegex(@"^-\s*\[x\]\s*\*\*Question\s+#(\d+):\*\*\s*(.+)$", RegexOptions.IgnoreCase, 2000)]
    private static partial Regex AnsweredQuestionHeaderRegex();

    [GeneratedRegex(@"^\s*\*\*Answer\s*\(([^)]+)\):\*\*\s*(.+)$", RegexOptions.IgnoreCase, 2000)]
    private static partial Regex AnswerLineRegex();

    [GeneratedRegex(@"^\s*\*\*Answer:\*\*\s*(.+)$", RegexOptions.IgnoreCase, 2000)]
    private static partial Regex UserAnswerLineRegex();

    [GeneratedRegex(@"^-\s*\[\s*\]\s*\*\*Question\s+#(\d+):\*\*\s*(.+)$", RegexOptions.IgnoreCase, 2000)]
    private static partial Regex OpenQuestionHeaderRegex();

    /// <summary>
    /// Parse open questions (unchecked) from markdown content
    /// </summary>
    public static List<OpenQuestion> ParseOpenQuestions(string markdown)
    {
        var results = new List<OpenQuestion>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return results;
        }

        var lines = markdown.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Look for open question header: - [ ] **Question #1:** Question text
            var match = OpenQuestionHeaderRegex().Match(line);
            if (match.Success)
            {
                var questionNumber = int.Parse(match.Groups[1].Value);
                var questionText = match.Groups[2].Value.Trim();

                results.Add(new OpenQuestion(questionNumber, questionText));
            }
        }

        return results;
    }

    /// <summary>
    /// Parse ambiguous questions from markdown content (from "Ambiguous Questions" section)
    /// </summary>
    public static List<OpenQuestion> ParseAmbiguousQuestions(string markdown)
    {
        var results = new List<OpenQuestion>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return results;
        }

        var lines = markdown.Split('\n');
        bool inAmbiguousSection = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Check if entering ambiguous questions section
            if (line.StartsWith("## Ambiguous Questions", StringComparison.OrdinalIgnoreCase))
            {
                inAmbiguousSection = true;
                continue;
            }

            // Check if leaving section (next section header)
            if (inAmbiguousSection && line.StartsWith("##"))
            {
                break;
            }

            // Parse ambiguous questions: - [ ] **Question #1:** Question text (unchecked only)
            // Answered ambiguous questions (- [x]) will be picked up by ParseAnsweredQuestions instead
            if (inAmbiguousSection && line.StartsWith("- [ ]"))
            {
                var match = Regex.Match(line, @"^-\s*\[\s*\]\s*\*\*Question\s+#(\d+):\*\*\s*(.+)$", RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    var questionNumber = int.Parse(match.Groups[1].Value);
                    var questionText = match.Groups[2].Value.Trim();
                    results.Add(new OpenQuestion(questionNumber, questionText));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Parse answered questions from markdown content
    /// </summary>
    public static List<AnsweredQuestion> ParseAnsweredQuestions(string markdown)
    {
        var results = new List<AnsweredQuestion>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return results;
        }

        var lines = markdown.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Look for answered question header: - [x] **Question #1:** Question text
            var headerMatch = AnsweredQuestionHeaderRegex().Match(line);
            if (headerMatch.Success)
            {
                var questionNumber = int.Parse(headerMatch.Groups[1].Value);
                var questionText = headerMatch.Groups[2].Value.Trim();

                // Look for answer on next line: **Answer (TechnicalAdvisor):** Answer text OR **Answer:** Answer text
                if (i + 1 < lines.Length)
                {
                    var answerLine = lines[i + 1].Trim();

                    // Try format with source: **Answer (TechnicalAdvisor):**
                    var answerMatch = AnswerLineRegex().Match(answerLine);
                    string answeredBy;
                    string answerText;

                    if (answerMatch.Success)
                    {
                        answeredBy = answerMatch.Groups[1].Value.Trim();
                        answerText = answerMatch.Groups[2].Value.Trim();
                    }
                    else
                    {
                        // Try format without source: **Answer:**
                        var userAnswerMatch = UserAnswerLineRegex().Match(answerLine);
                        if (userAnswerMatch.Success)
                        {
                            answeredBy = "User";
                            answerText = userAnswerMatch.Groups[1].Value.Trim();
                        }
                        else
                        {
                            // No valid answer format found
                            continue;
                        }
                    }

                    // Collect multi-line answers
                    var fullAnswer = answerText;
                    int j = i + 2;
                    while (j < lines.Length)
                    {
                        var nextLine = lines[j].Trim();
                        // Stop if we hit another question or section header
                        if (nextLine.StartsWith("- [") || nextLine.StartsWith("#") || string.IsNullOrWhiteSpace(nextLine))
                        {
                            break;
                        }
                        fullAnswer += " " + nextLine;
                        j++;
                    }

                    results.Add(new AnsweredQuestion(
                        QuestionNumber: questionNumber,
                        Question: questionText,
                        Answer: fullAnswer,
                        AnsweredBy: answeredBy
                    ));

                    i = j - 1; // Skip processed lines
                }
            }
        }

        return results;
    }
}
