using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class SocialSituationDetector : ISocialSituationDetector
{
    public SocialSituation Detect(MessageContext context)
    {
        var source = context.SourceText ?? string.Empty;

        if (ContainsAny(source,
                "joined", "joining", "new role", "new chapter", "excited to share",
                "grateful", "gratitude", "happy to share", "thrilled to share",
                "promotion", "milestone", "journey", "years at"))
        {
            return new SocialSituation
            {
                Type = "milestone",
                Confidence = 0.90,
                Signals = new[] { "career milestone", "gratitude", "new beginning", "isPublicCelebration" }
            };
        }

        if (ContainsAny(source,
                "hiring", "we're hiring", "we are hiring", "job opening", "career opportunity"))
        {
            return new SocialSituation
            {
                Type = "hiring",
                Confidence = 0.85,
                Signals = new[] { "job opportunity", "career transition", "containsCareerTransition" }
            };
        }

        if (ContainsAny(source,
                "what is", "pattern", "architecture", "key takeaway", "example",
                "why use", "types of", "implementation", "real-world example",
                "breakdown", "understanding", "microservices", "system design"))
        {
            return new SocialSituation
            {
                Type = "educational",
                Confidence = 0.88,
                Signals = new[] { "explanation", "breakdown", "teaching" }
            };
        }

        if (ContainsAny(source, "i believe", "i think", "in my view", "the key is", "important", "should"))
        {
            return new SocialSituation
            {
                Type = "opinion",
                Confidence = 0.76,
                Signals = new[] { "opinion", "stance", "asksForDiscussion" }
            };
        }

        if (source.Contains("?", StringComparison.Ordinal) ||
            ContainsAny(source, "what do you think", "curious", "how do you", "would you"))
        {
            return new SocialSituation
            {
                Type = "question",
                Confidence = 0.78,
                Signals = new[] { "question", "discussion prompt" }
            };
        }

        if (ContainsAny(source, "just wanted to share", "quick update", "thought you might like to know"))
        {
            return new SocialSituation
            {
                Type = "personal_update",
                Confidence = 0.70,
                Signals = new[] { "soft_signal", "personal update" }
            };
        }

        if (ContainsAny(source, "good morning", "have a great day", "happy monday", "weekend recap"))
        {
            return new SocialSituation
            {
                Type = "casual",
                Confidence = 0.70,
                Signals = new[] { "casual greeting", "low engagement", "isCasual" }
            };
        }

        return new SocialSituation
        {
            Type = "general",
            Confidence = 0.55
        };
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));
}
