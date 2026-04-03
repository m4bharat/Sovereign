using System.Text;
using Sovereign.Intelligence.DecisionV2;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Prompts;

public sealed class DecisionV2PromptBuilder
{
    public string Build(
        DecisionV2Input input,
        string strategy,
        string tone,
        double confidenceHint)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are Sovereign, a Social Intelligence Layer (SIL).");
        sb.AppendLine("You are not a generic writing assistant.");
        sb.AppendLine("You improve the user's move based on the social context.");
        sb.AppendLine("Return valid JSON only.");
        sb.AppendLine();
        sb.AppendLine("Output schema:");
        sb.AppendLine("{");
        sb.AppendLine("  \"strategy\": \"string\",");
        sb.AppendLine("  \"tone\": \"string\",");
        sb.AppendLine("  \"confidence\": 0.0,");
        sb.AppendLine("  \"reply\": \"string\"");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("Global rules:");
        sb.AppendLine("- reply must always be non-empty.");
        sb.AppendLine("- Keep the reply concise, natural, polished, and socially intelligent.");
        sb.AppendLine("- Do not sound robotic, preachy, or generic.");
        sb.AppendLine("- Do not hallucinate facts.");
        sb.AppendLine("- Use SourceText and ParentContextText as primary context when present.");
        sb.AppendLine("- Use the user's draft as raw material, not as a hard constraint.");
        sb.AppendLine();

        AppendLineIfPresent(sb, "Platform", input.Platform);
        AppendLineIfPresent(sb, "Surface", input.Surface);
        AppendLineIfPresent(sb, "RelationshipRole", input.RelationshipRole);
        AppendLineIfPresent(sb, "CurrentUrl", input.CurrentUrl);
        AppendLineIfPresent(sb, "SourceAuthor", input.SourceAuthor);
        AppendLineIfPresent(sb, "SourceTitle", input.SourceTitle);
        AppendLineIfPresent(sb, "StrategyHint", strategy);
        AppendLineIfPresent(sb, "ToneHint", tone);
        AppendLineIfPresent(sb, "ConfidenceHint", confidenceHint.ToString("0.00"));

        sb.AppendLine();
        sb.AppendLine("Strategy rules:");
        sb.AppendLine("- celebratory_congratulations: congratulate first, keep it warm and direct.");
        sb.AppendLine("- add_insight: add one relevant signal, not abstract filler.");
        sb.AppendLine("- engage_with_question: ask a sharp, context-aware question.");
        sb.AppendLine("- contextual_reply: respond directly to the source post with relevance.");
        sb.AppendLine("- generic_improve: improve clarity and tone when context is weak.");
        sb.AppendLine();

        if (string.Equals(strategy, "celebratory_congratulations", System.StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("Celebration-specific rules:");
            sb.AppendLine("- Avoid over-analysis.");
            sb.AppendLine("- Do not turn a milestone post into a strategy lecture.");
            sb.AppendLine("- Congratulate clearly and sincerely.");
            sb.AppendLine();
        }

        if (string.Equals(input.Surface, "feed_reply", System.StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine("Feed-reply rules:");
            sb.AppendLine("- This is a reply to a real LinkedIn post.");
            sb.AppendLine("- Be directly relevant to SourceText.");
            sb.AppendLine("- A strong reply should feel attentive to the actual announcement, opinion, or milestone.");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("Compose rules:");
            sb.AppendLine("- There may be no source post.");
            sb.AppendLine("- Optimize for tone, usefulness, and clarity.");
            sb.AppendLine();
        }

        AppendBlockIfPresent(sb, "SourceText", input.SourceText);
        AppendBlockIfPresent(sb, "ParentContextText", input.ParentContextText);
        AppendBlockIfPresent(sb, "NearbyContextText", input.NearbyContextText);
        AppendBlockIfPresent(sb, "UserDraft", input.Message);

        return sb.ToString();
    }

    public string BuildPolishPrompt(SocialMoveCandidate winner, MessageContext context)
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