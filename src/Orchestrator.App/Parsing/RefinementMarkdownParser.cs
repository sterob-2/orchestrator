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

                // Look for answer on next line: **Answer (TechnicalAdvisor):** Answer text
                if (i + 1 < lines.Length)
                {
                    var answerLine = lines[i + 1].Trim();
                    var answerMatch = AnswerLineRegex().Match(answerLine);

                    if (answerMatch.Success)
                    {
                        var answeredBy = answerMatch.Groups[1].Value.Trim();
                        var answerText = answerMatch.Groups[2].Value.Trim();

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
        }

        return results;
    }
}
