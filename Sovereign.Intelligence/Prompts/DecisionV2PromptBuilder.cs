using System.Linq;
using System.Text;
using Sovereign.Domain.Models;
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

        sb.AppendLine("Rules:");
        sb.AppendLine("- Keep the user's intent, but improve the move.");
        sb.AppendLine("- Keep the reply concise, human, specific, and socially intelligent.");
        sb.AppendLine("- Never sound robotic, preachy, or generic.");
        sb.AppendLine("- Never hallucinate facts.");
        sb.AppendLine("- Ban filler such as: 'Great point', 'So important', 'Thanks for sharing', 'Well said' unless grounded in specific context.");
        sb.AppendLine("- Respect interaction mode:");
        sb.AppendLine("  - chat: respond like a natural direct message.");
        sb.AppendLine("  - reply: respond to the source post/comment directly.");
        sb.AppendLine("  - compose: produce a clean standalone draft.");
        sb.AppendLine("- If move is no_reply, return an empty reply.");
        sb.AppendLine();

        AppendLineIfPresent(sb, "InteractionMode", context.InteractionMode);
        AppendLineIfPresent(sb, "SituationType", context.SituationType);
        AppendLineIfPresent(sb, "DesiredTone", context.DesiredTone);
        AppendLineIfPresent(sb, "ChosenMove", winner.Move);
        AppendLineIfPresent(sb, "Rationale", winner.Rationale);
        AppendLineIfPresent(sb, "Author", context.SourceAuthor);
        AppendLineIfPresent(sb, "Title", context.SourceTitle);

        AppendBlockIfPresent(sb, "UserDraft", context.Message);
        AppendBlockIfPresent(sb, "SourceText", context.SourceText);
        AppendBlockIfPresent(sb, "ParentContextText", context.ParentContextText);
        AppendBlockIfPresent(sb, "NearbyContextText", context.NearbyContextText);

        if (context.RecentMessages?.Any() == true && context.InteractionMode == "chat")
        {
            sb.AppendLine("RecentMessages:");
            foreach (var line in context.RecentMessages.TakeLast(6))
            {
                sb.AppendLine($"- {line}");
            }
            sb.AppendLine();
        }

        if (context.MemoryFacts?.Any() == true && context.InteractionMode != "reply")
        {
            sb.AppendLine("RelevantMemoryFacts:");
            foreach (var fact in context.MemoryFacts.Take(5))
            {
                sb.AppendLine($"- {fact}");
            }
            sb.AppendLine();
        }

        AppendBlockIfPresent(sb, "CandidateReply", winner.Reply);

        if (winner.Alternatives?.Any() == true)
        {
            sb.AppendLine("Alternatives:");
            foreach (var alt in winner.Alternatives.Take(3))
            {
                sb.AppendLine($"- {alt}");
            }
        }

        return sb.ToString();
    }

    private static void AppendLineIfPresent(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine($"{label}: {value}");
        }
    }

    private static void AppendBlockIfPresent(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            sb.AppendLine($"{label}:");
            sb.AppendLine(value.Trim());
            sb.AppendLine();
        }
    }
}
