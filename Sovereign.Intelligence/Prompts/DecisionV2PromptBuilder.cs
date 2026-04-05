using System.Text;
using Sovereign.Intelligence.DecisionV2;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Prompts;

public sealed class DecisionV2PromptBuilder
{
    public string Build(SocialMoveCandidate winner, MessageContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are Sovereign, a Social Intelligence Layer (SIL).");
        sb.AppendLine("You are not a generic writing assistant.");
        sb.AppendLine("You improve the user's move based on the social context.");
        sb.AppendLine("Return valid JSON only.");
        sb.AppendLine();
        sb.AppendLine("Output schema:");
        sb.AppendLine("{");
        sb.AppendLine("  \"reply\": \"string\",");
        sb.AppendLine("  \"confidence\": 0.0,");
        sb.AppendLine("  \"brief_rationale\": \"string\",");
        sb.AppendLine("  \"alternative_rewrites\": [\"string\"]");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("Global rules:");
        sb.AppendLine("- Improve the reply without changing its intent.");
        sb.AppendLine("- Keep the reply concise, natural, polished, and socially intelligent.");
        sb.AppendLine("- Do not sound robotic, preachy, or generic.");
        sb.AppendLine("- Do not hallucinate facts.");
        sb.AppendLine("- Use SourceText and ParentContextText as primary context when present.");
        sb.AppendLine("- For questions on high-signal posts (educational, opinion, recruitment, milestone, opportunity):");
        sb.AppendLine("  - Never emit bare questions. Always include framing statements first.");
        sb.AppendLine("  - Use specific angles: constraint, trade-off, pattern, transition, selection.");
        sb.AppendLine("  - Ban generic stems like 'What do you think?', 'Can you share more?', 'What's the biggest challenge?'.");
        sb.AppendLine("  - Examples: 'Programs like this close the learning-to-production gap well. I'm curious—what tends to be the hardest part when graduates first enter real client work?'");
        sb.AppendLine();

        AppendLineIfPresent(sb, "SituationType", context.SituationType);
        AppendLineIfPresent(sb, "ChosenMove", winner.Move);
        AppendLineIfPresent(sb, "Rationale", winner.Rationale);
        AppendLineIfPresent(sb, "DesiredTone", context.DesiredTone);
        AppendLineIfPresent(sb, "SourceText", context.SourceText);
        AppendLineIfPresent(sb, "Author", context.SourceAuthor);
        AppendBlockIfPresent(sb, "CandidateReply", winner.Reply);

        if (winner.Alternatives != null && winner.Alternatives.Any())
        {
            sb.AppendLine("Alternatives:");
            foreach (var alternative in winner.Alternatives)
            {
                sb.AppendLine($"  {{\"reply\":\"{alternative}\"}}");
            }
        }

        return sb.ToString();
    }

    private static void AppendLineIfPresent(StringBuilder sb, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine($"{label}: {value}");
        }
    }

    private static void AppendBlockIfPresent(StringBuilder sb, string label, string value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine($"{label}:");
            sb.AppendLine(value.Trim());
        }
    }
}