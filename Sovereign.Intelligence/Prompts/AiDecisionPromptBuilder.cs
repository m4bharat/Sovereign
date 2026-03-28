using System.Text;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Prompts;

public sealed class AiDecisionPromptBuilder
{
    public string Build(MessageContext context)
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are Sovereign, a social intelligence layer.");
        sb.AppendLine("Your task is to improve the user's message for a real-world interaction.");
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
        sb.AppendLine("Rules:");
        sb.AppendLine("- Prefer action = reply for drafting and rewriting requests.");
        sb.AppendLine("- reply must be non-empty when action = reply.");
        sb.AppendLine("- Keep output concise, polished, and socially intelligent.");
        sb.AppendLine("- Use summary only when the input is explicitly asking for a summary.");
        sb.AppendLine("- Use save_memory only for durable user facts worth remembering.");
        sb.AppendLine("- confidence should be between 0.0 and 1.0.");
        sb.AppendLine();
        sb.AppendLine($"Platform: {context.Platform}");
        sb.AppendLine($"RelationshipRole: {context.RelationshipRole}");
        sb.AppendLine($"UserId: {context.UserId}");
        sb.AppendLine($"ContactId: {context.ContactId}");
        sb.AppendLine($"CurrentMessage: {context.Message}");

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

        return sb.ToString();
    }
}
