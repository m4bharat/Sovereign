using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Prompts;

public sealed class AiDecisionPromptBuilder
{
    public string Build(MessageContext context)
    {
        return $@"
You are an AI messaging decision engine.

Return valid JSON only.
Allowed actions:
- reply
- save_memory
- summarize
- no_action

Schema:
{{
  ""action"": ""reply|save_memory|summarize|no_action"",
  ""reply"": """",
  ""memoryKey"": """",
  ""memoryValue"": """",
  ""summary"": """",
  ""confidence"": 0.0
}}

Rules:
- If the message contains a fact that should be remembered, choose save_memory.
- If the message asks for summary or recap, choose summarize.
- If the message should be answered conversationally, choose reply.
- If nothing should happen, choose no_action.

Context:
UserId: {context.UserId}
ContactId: {context.ContactId}
RelationshipRole: {context.RelationshipRole}
RecentSummary: {context.RecentSummary}
LastTopicSummary: {context.LastTopicSummary}
Message: {context.Message}
";
    }
}
