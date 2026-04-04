using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;
using System.Text.RegularExpressions;

namespace Sovereign.Intelligence.Services;

public sealed class CandidateReplyGenerator : ICandidateReplyGenerator
{
    public IReadOnlyList<SocialMoveCandidate> Generate(
        IReadOnlyList<SocialMoveCandidate> moveCandidates,
        MessageContext context)
    {
        return moveCandidates
            .Select(move => new SocialMoveCandidate
            {
                Move = move.Move,
                Rationale = move.Rationale,
                Reply = BuildReply(context, move.Move),
                ShortReply = BuildShortReply(context, move.Move),
                GenerationConfidence = 0.85,
                RequiresPolish = true,
            })
            .ToArray();
    }

    private static string BuildReply(MessageContext context, string move)
    {
        var author = context.SourceAuthor?.Trim();
        var source = context.SourceText ?? string.Empty;
        var topic = ExtractTopic(source);

        return move switch
        {
            "congratulate" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Congratulations on this exciting milestone. Wishing you all the best ahead."
                    : $"Congratulations, {author}! Wishing you all the best ahead.",

            "congratulate_encourage" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Congratulations on this milestone! Keep up the great work ahead."
                    : $"Congratulations, {author}! Keep pushing forward with that momentum.",

            "appreciate_journey" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Impressive journey to get here. Respect the dedication."
                    : $"Impressive journey, {author}. Respect the dedication it took.",

            "express_interest" =>
                "This looks like an interesting opportunity. I'd love to learn more.",

            "amplify_signal" =>
                "Sharing this great opportunity with my network. Looks promising!",

            "offer_support" =>
                "If you need any connections or advice, I'm here to help.",

            "add_insight" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "Great perspective. I'd add that context is key in these situations."
                    : $"Great perspective on {topic}. I'd add that context is key.",

            "agree" =>
                "I agree with this take. Well said.",

            "add_nuance" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "I agree, and I'd add that timing often plays a role too."
                    : $"I agree, and I'd add that timing often plays a role with {topic}.",

            "answer_supportively" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "That's a great question. In my experience, it depends on the context."
                    : $"That's a thoughtful question about {topic}. In my experience, finding the right balance comes down to clarity and boundaries.",

            "acknowledge_update" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "Thanks for the update. Good to hear from you."
                    : $"Thanks for the update on {topic}. Good to hear from you.",

            "encourage" =>
                "Keep it up! You're doing great work.",

            "appreciate" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "Really clear breakdown — easy to connect to real-world use."
                    : $"Really clear breakdown of {topic} — easy to connect to real-world systems.",

            "ask_relevant_question" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "Curious how you’ve seen this play out in practice?"
                    : $"Curious — what’s the hardest part of applying {topic} in real systems?",

            "defer" =>
                "Impressive achievement. Well deserved.",

            "direct_message" =>
                "Let's take this to DM to discuss further.",

            "ask_details" =>
                "Could you share a few more details so I can understand it better?",

            "engage_privately" =>
                "This feels worth discussing privately — can we continue this in DM?",

            "light_touch" =>
                "Noted. Keep up the good work.",

            "engage" =>
                "This resonates. I'd love to hear more about your perspective.",

            "praise" =>
                "Outstanding work. Truly impressive.",

            "respond" =>
                "Thanks for sharing. Happy holidays to you too.",

            "acknowledge" =>
                "Thanks for the update. Noted.",

            "no_reply" =>
                string.Empty,

            _ => "Appreciate you sharing this — thoughtful framing."
        };
    }

    private static string BuildShortReply(MessageContext context, string move)
    {
        var author = context.SourceAuthor?.Trim();

        return move switch
        {
            "congratulate" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Congrats on the milestone!"
                    : $"Congrats, {author}!",

            "appreciate" => "Great breakdown — thanks for sharing!",

            "ask_relevant_question" => "Curious — how does this apply in practice?",

            _ => "Thanks for sharing!"
        };
    }

    private static string ExtractTopic(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return string.Empty;
        }

        var patterns = new[]
        {
            @"\b(?:how do you|how can you|how would you)\s+(?:handle|manage|approach|think about)\s+([a-zA-Z0-9\s\-]+?)(?:[\?\.!]|$)",
            @"\b(?:what(?:'s| is) your take on|what do you think about|what are your thoughts on)\s+([a-zA-Z0-9\s\-]+?)(?:[\?\.!]|$)",
            @"\b(?:about|on|of|for|in|regarding)\s+([a-zA-Z0-9\s\-]+?)(?:[\?\.!]|$)",
            @"\b(topic|concept|idea):\s*([a-zA-Z0-9\s\-]+)"
        };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(source, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                var groupValue = match.Groups.Count > 2 ? match.Groups[2].Value : match.Groups[1].Value;
                var topic = Regex.Replace(groupValue.Trim(), "\\s+", " ");
                if (topic.Length > 0 && topic.Length <= 60)
                {
                    return topic;
                }
            }
        }

        var fallback = Regex.Match(source, @"\b([A-Za-z0-9\-]{4,})(?:\s+[A-Za-z0-9\-]{4,})?", RegexOptions.IgnoreCase);
        return fallback.Success ? fallback.Value : string.Empty;
    }
}