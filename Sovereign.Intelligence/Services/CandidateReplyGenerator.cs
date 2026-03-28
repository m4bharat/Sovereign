using Sovereign.Intelligence.Models;
using System.Text.RegularExpressions;

namespace Sovereign.Intelligence.Services;

public sealed class CandidateReplyGenerator
{
    public IReadOnlyList<SocialMoveCandidate> Generate(
        MessageContext context,
        IReadOnlyList<SocialMoveCandidate> moveCandidates,
        SocialSituation situation)
    {
        return moveCandidates
            .Select(move => new SocialMoveCandidate
            {
                Move = move.Move,
                Rationale = move.Rationale,
                Reply = BuildReply(context, move.Move, situation.Type),
                GenerationConfidence = 0.70
            })
            .ToArray();
    }

    private static string BuildReply(MessageContext context, string move, string situationType)
    {
        var author = context.SourceAuthor?.Trim();
        var source = context.SourceText ?? string.Empty;
        var company = ExtractCompany(source);
        var topic = ExtractTopic(source);

        return move switch
        {
            "congratulate" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Congratulations on this exciting milestone. Wishing you all the very best for what comes next."
                    : $"Congratulations on this exciting milestone, {author}! Wishing you all the very best for what comes next.",

            "congratulate_encourage" =>
                string.IsNullOrWhiteSpace(author)
                    ? BuildCongratsEncourageNoAuthor(company)
                    : BuildCongratsEncourage(author, company),

            "appreciate_journey" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Your journey and the gratitude in your post really stand out — wishing you all the very best for this next chapter."
                    : $"Your journey and the gratitude in your post really stand out, {author} — wishing you all the very best for this next chapter.",

            "appreciate" =>
                string.IsNullOrWhiteSpace(topic)
                    ? (string.IsNullOrWhiteSpace(author)
                        ? "Really clear breakdown — the practical framing makes it easy to connect the idea to real-world use."
                        : $"Really clear breakdown, {author} — the practical framing makes it easy to connect the idea to real-world use.")
                    : (string.IsNullOrWhiteSpace(author)
                        ? $"Really clear breakdown of {topic} — the practical framing makes it easy to connect the concept to real-world systems."
                        : $"Really clear breakdown, {author} — the way you explained {topic} makes it easy to connect the concept to real-world systems."),

            "add_insight" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "Strong explanation — this is exactly the kind of concept that becomes much clearer when tied to practical tradeoffs."
                    : $"Strong explanation — the real value of {topic} is how clearly it connects theory to practical tradeoffs in real systems.",

            "ask_relevant_question" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "Really thoughtful perspective. Curious how you’ve seen this play out in practice?"
                    : $"Really thoughtful breakdown of {topic}. Curious — in your experience, what tends to be the hardest part when applying this in real systems?",

            "agree" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Strong point — the way you framed this makes the core idea easy to relate to in practice."
                    : $"Strong point, {author} — the way you framed this makes the core idea easy to relate to in practice.",

            "add_nuance" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Strong point — and what makes it especially interesting is how context changes the tradeoffs in practice."
                    : $"Strong point, {author} — and what makes it especially interesting is how context changes the tradeoffs in practice.",

            "answer_supportively" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Interesting question — I like the way you framed it because it opens up a very practical discussion."
                    : $"Interesting question, {author} — I like the way you framed it because it opens up a very practical discussion.",

            "encourage" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Appreciate you sharing this — wishing you all the best ahead."
                    : $"Appreciate you sharing this, {author} — wishing you all the best ahead.",

            _ =>
                string.IsNullOrWhiteSpace(author)
                    ? "Appreciate you sharing this — there is a lot of signal in the way you framed it."
                    : $"Appreciate you sharing this, {author} — there is a lot of signal in the way you framed it."
        };
    }

    private static string BuildCongratsEncourage(string author, string company)
    {
        if (!string.IsNullOrWhiteSpace(company))
        {
            return $"Congratulations on joining {company}, {author}! Wishing you all the very best as you begin this exciting new chapter.";
        }

        return $"Congratulations on this exciting new chapter, {author}! Wishing you all the very best for what lies ahead.";
    }

    private static string BuildCongratsEncourageNoAuthor(string company)
    {
        if (!string.IsNullOrWhiteSpace(company))
        {
            return $"Congratulations on joining {company}! Wishing you all the very best as you begin this exciting new chapter.";
        }

        return "Congratulations on this exciting new chapter. Wishing you all the very best for what lies ahead.";
    }

    private static string ExtractCompany(string sourceText)
    {
        var match = Regex.Match(sourceText, @"\b(joined|joining|at)\s+([A-Z][A-Za-z0-9&.-]+(?:\s+[A-Z][A-Za-z0-9&.-]+)*)");
        if (match.Success && match.Groups.Count > 2)
        {
            return match.Groups[2].Value.Trim();
        }

        return string.Empty;
    }

    private static string ExtractTopic(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return string.Empty;
        }

        var firstSentence = sourceText
            .Split(new[] { '.', '!', '?', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(firstSentence))
        {
            return string.Empty;
        }

        var cleaned = Regex.Replace(firstSentence, @"^understanding\s+", "", RegexOptions.IgnoreCase).Trim();
        cleaned = Regex.Replace(cleaned, @"^what\s+is\s+", "", RegexOptions.IgnoreCase).Trim();
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        return cleaned.Length > 90 ? cleaned[..90].Trim() : cleaned;
    }
}