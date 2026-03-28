using System.Text.RegularExpressions;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class SocialMoveSelectionEngine
{
    public SocialMoveResult Select(MessageContext context)
    {
        if (!string.Equals(context.InteractionMode, "reply", StringComparison.OrdinalIgnoreCase))
        {
            return new SocialMoveResult("default", BuildDefaultReply(context));
        }

        var source = context.SourceText ?? string.Empty;
        var author = context.SourceAuthor?.Trim() ?? string.Empty;

        if (LooksLikeMilestone(source))
        {
            return new SocialMoveResult(
                "congratulate",
                string.IsNullOrWhiteSpace(author)
                    ? "Congratulations on this exciting new chapter. Your journey and the gratitude in your post really stand out — wishing you all the very best for what comes next."
                    : $"Congratulations on this exciting new chapter, {author}. Your journey and the gratitude in your post really stand out — wishing you all the very best for what comes next.");
        }

        if (LooksLikeEducationalBreakdown(source))
        {
            var topic = ExtractTopic(source);
            var topicPhrase = string.IsNullOrWhiteSpace(topic) ? "this" : topic;

            return new SocialMoveResult(
                "appreciate_add_insight",
                string.IsNullOrWhiteSpace(author)
                    ? $"Really clear breakdown of {topicPhrase} — the practical framing makes it easy to connect the concept to real-world systems."
                    : $"Really clear breakdown, {author} — the way you explained {topicPhrase} makes it easy to connect the concept to real-world systems.");
        }

        if (LooksLikeQuestion(source))
        {
            var topic = ExtractTopic(source);
            var topicPhrase = string.IsNullOrWhiteSpace(topic) ? "this" : topic;

            return new SocialMoveResult(
                "answer_or_question",
                string.IsNullOrWhiteSpace(author)
                    ? $"Interesting perspective on {topicPhrase}. I especially like how you framed it in practical terms."
                    : $"Interesting perspective, {author}. I especially like how you framed {topicPhrase} in practical terms.");
        }

        if (LooksLikeOpinion(source))
        {
            return new SocialMoveResult(
                "agree",
                string.IsNullOrWhiteSpace(author)
                    ? "Strong point — the way you framed this makes the core idea easy to relate to in practice."
                    : $"Strong point, {author} — the way you framed this makes the core idea easy to relate to in practice.");
        }

        return new SocialMoveResult("appreciate", BuildDefaultReply(context));
    }

    private static string BuildDefaultReply(MessageContext context)
    {
        var author = context.SourceAuthor?.Trim() ?? string.Empty;
        return string.IsNullOrWhiteSpace(author)
            ? "Appreciate you sharing this — there is a lot of signal in the way you framed it."
            : $"Appreciate you sharing this, {author} — there is a lot of signal in the way you framed it.";
    }

    private static bool LooksLikeMilestone(string text)
    {
        return ContainsAny(text,
            "new role", "new chapter", "starting a new role", "journey", "grateful", "gratitude",
            "excited to share", "beginning", "years at", "joining", "joined", "moved on", "promotion");
    }

    private static bool LooksLikeEducationalBreakdown(string text)
    {
        return ContainsAny(text,
            "what is", "pattern", "architecture", "key takeaway", "example", "why use", "types of",
            "implementation", "real-world example", "breakdown", "understanding", "microservices");
    }

    private static bool LooksLikeQuestion(string text)
    {
        return text.Contains("?", StringComparison.Ordinal) ||
               ContainsAny(text, "what do you think", "curious", "how do you", "would you");
    }

    private static bool LooksLikeOpinion(string text)
    {
        return ContainsAny(text, "i believe", "i think", "in my view", "the key is", "important", "should");
    }

    private static bool ContainsAny(string text, params string[] terms)
    {
        return terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    private static string ExtractTopic(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        var firstSentence = sourceText
            .Split(new[] {'.', '!', '?', '\n'}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(firstSentence))
        {
            return string.Empty;
        }

        var cleaned = Regex.Replace(firstSentence, @"^understanding\s+", "", RegexOptions.IgnoreCase).Trim();
        cleaned = Regex.Replace(cleaned, @"^what\s+is\s+", "", RegexOptions.IgnoreCase).Trim();

        return cleaned.Length > 80 ? cleaned[..80].Trim() : cleaned;
    }
}

public sealed record SocialMoveResult(string Move, string Reply);
