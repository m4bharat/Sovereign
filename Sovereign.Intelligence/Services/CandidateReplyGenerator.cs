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
                "I agree, and I'd add that timing often plays a role too.",

            "answer_supportively" =>
                "That's a great question. In my experience, it depends on the context.",

            "acknowledge_update" =>
                "Thanks for the update. Good to hear from you.",

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
        // Simplified topic extraction logic
        var match = Regex.Match(source, @"\b(topic|concept|idea):\s*(\w+)", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[2].Value : string.Empty;
    }
}