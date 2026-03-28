using System.Text;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Prompts;

public sealed class AiDecisionPromptBuilder
{
    public string Build(MessageContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are Sovereign, a Social Intelligence Layer (SIL).");
        sb.AppendLine("Your job is not to sound impressive. Your job is to understand the room and upgrade the user's move.");
        sb.AppendLine("Return valid JSON only.");
        sb.AppendLine();
        sb.AppendLine("Output schema:");
        sb.AppendLine("{");
        sb.AppendLine("  \"action\": \"reply\" | \"save_memory\" | \"summarize\",");
        sb.AppendLine("  \"reply\": \"string\",");
        sb.AppendLine("  \"memoryKey\": \"string\",");
        sb.AppendLine("  \"memoryValue\": \"string\",");
        sb.AppendLine("  \"summary\": \"string\",");
        sb.AppendLine("  \"confidence\": 0.0");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("Global rules:");
        sb.AppendLine("- Prefer action = reply for drafting and rewriting requests.");
        sb.AppendLine("- reply must be non-empty when action = reply.");
        sb.AppendLine("- Keep output concise, polished, natural, and socially intelligent.");
        sb.AppendLine("- Accuracy matters more than sounding impressive.");
        sb.AppendLine("- Do not invent roles, industries, facts, timelines, or achievements not present in the provided context.");
        sb.AppendLine("- Do not critique or suggest improvements to a public post unless explicitly asked.");
        sb.AppendLine("- Use save_memory only for durable user facts worth remembering.");
        sb.AppendLine("- Use summary only when the input explicitly asks for a summary.");
        sb.AppendLine("- confidence should be between 0.0 and 1.0.");
        sb.AppendLine();

        AppendLineIfPresent(sb, "InteractionMode", context.InteractionMode);
        AppendLineIfPresent(sb, "Platform", context.Platform);
        AppendLineIfPresent(sb, "Surface", context.Surface);
        AppendLineIfPresent(sb, "RelationshipRole", context.RelationshipRole);
        AppendLineIfPresent(sb, "CurrentUrl", context.CurrentUrl);
        AppendLineIfPresent(sb, "SourceAuthor", context.SourceAuthor);
        AppendLineIfPresent(sb, "SourceTitle", context.SourceTitle);

        if (string.Equals(context.InteractionMode, "reply", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine();
            sb.AppendLine("Reply-mode rules:");
            sb.AppendLine("- This is a reply to a specific source post, source comment, or source message.");
            sb.AppendLine("- The reply MUST be directly relevant to SourceText.");
            sb.AppendLine("- If the user's draft is generic or off-topic, rewrite it so it clearly responds to the SourceText.");
            sb.AppendLine("- Do NOT introduce facts or assumptions not present in SourceText, SourceAuthor, SourceTitle, or CurrentMessage.");
            sb.AppendLine("- Do NOT add domain assumptions like industry, function, geography, or technology unless explicitly present.");
            sb.AppendLine("- Your role is to respond as a participant in the conversation, not as an editor of the post.");
            sb.AppendLine("- Choose the best social move for the room: congratulate, appreciate, agree, add insight, ask a relevant question, or encourage.");
            sb.AppendLine("- Priority order: SourceText, then CurrentMessage, then NearbyContextText.");
            sb.AppendLine("- A good reply should feel attentive to the real milestone, opinion, announcement, question, or update in SourceText.");
        }
        else if (string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase))
        {
            sb.AppendLine();
            sb.AppendLine("Chat-mode rules:");
            sb.AppendLine("- Maintain continuity and respond naturally to the current thread.");
            sb.AppendLine("- Optimize for natural flow, tone alignment, and forward progress in the conversation.");
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine("Compose-mode rules:");
            sb.AppendLine("- There may be no source post.");
            sb.AppendLine("- Optimize for clarity, tone, confidence, and usefulness.");
        }

        AppendBlockIfPresent(sb, "SourceText", context.SourceText);
        AppendBlockIfPresent(sb, "ParentContextText", context.ParentContextText);
        AppendBlockIfPresent(sb, "NearbyContextText", context.NearbyContextText);
        AppendLineIfPresent(sb, "CurrentMessage", context.Message);

        if (context.MemoryFacts.Count > 0)
        {
            sb.AppendLine("MemoryFacts:");
            foreach (var memory in context.MemoryFacts)
            {
                sb.AppendLine($"- {memory}");
            }
        }

        if (context.RecentMessages.Count > 0)
        {
            sb.AppendLine("RecentMessages:");
            foreach (var recentMessage in context.RecentMessages)
            {
                sb.AppendLine($"- {recentMessage}");
            }
        }

        AppendBlockIfPresent(sb, "RecentSummary", context.RecentSummary);
        AppendBlockIfPresent(sb, "LastTopicSummary", context.LastTopicSummary);
        AppendBlockIfPresent(sb, "RelevantMemories", context.RelevantMemories);

        if (context.InteractionMetadata.Count > 0)
        {
            sb.AppendLine("InteractionMetadata:");
            foreach (var pair in context.InteractionMetadata)
            {
                sb.AppendLine($"- {pair.Key}: {pair.Value}");
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
