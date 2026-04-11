using System.Linq;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class SocialSituationDetector : ISocialSituationDetector
{
    private static readonly string[] CtaMarkers =
    {
        "how ",
        "what ",
        "why ",
        "curious",
        "would love to hear",
        "thoughts?",
        "any advice",
        "looking for",
        "seeking",
        "can anyone",
        "has anyone",
        "what do you think",
        "would you do",
        "anyone else"
    };

    private static bool LooksLikeCtaOrQuestion(MessageContext context)
    {
        var source = (context.SourceText ?? string.Empty).Trim().ToLowerInvariant();
        var draft = (context.Message ?? string.Empty).Trim().ToLowerInvariant();

        if (source.Contains("?") || draft.Contains("?"))
            return true;

        foreach (var marker in CtaMarkers)
        {
            if (source.Contains(marker) || draft.Contains(marker))
                return true;
        }

        return false;
    }

    public SocialSituation Detect(MessageContext context)
    {
        if ((context.InteractionMode ?? string.Empty).Equals("chat", StringComparison.OrdinalIgnoreCase))
        {
            return new SocialSituation
            {
                Type = "direct_message",
                Summary = "This is a messaging/chat interaction that should sound natural and direct."
            };
        }

        if (LooksLikeCtaOrQuestion(context))
        {
            return new SocialSituation
            {
                Type = "cta_or_question",
                Summary = "The source is asking for input, advice, or direct engagement."
            };
        }

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

        if (ContainsAny(source, "just wanted to share", "quick update", "thought you might like to know"))
        {
            return new SocialSituation
            {
                Type = "personal_update",
                Confidence = 0.70,
                Signals = new[] { "soft_signal", "personal update" }
            };
        }

        if (ContainsAny(source,
                "reply later", "i'll reply later", "will reply later", "respond later", "reply when i", "more details soon"))
        {
            return new SocialSituation
            {
                Type = "no_reply",
                Confidence = 0.95,
                Signals = new[] { "defer response", "wait to reply", "low signal" }
            };
        }

        if (ContainsAny(source,
                "dm", "direct message", "message me", "reach out privately", "discuss this privately", "private conversation"))
        {
            return new SocialSituation
            {
                Type = "direct_message",
                Confidence = 0.88,
                Signals = new[] { "private outreach", "direct conversation" }
            };
        }

        if (ContainsAny(source,
                "funding", "series a", "series b", "investment", "raised", "backers",
                "thrilled to announce", "proud to share", "amazing team"))
        {
            return new SocialSituation
            {
                Type = "celebratory",
                Confidence = 0.85,
                Signals = new[] { "funding milestone", "company success", "high status" }
            };
        }

        if (ContainsAny(source,
                "reflecting on", "past year", "career journey", "looking back",
                "grateful for connections", "it's been"))
        {
            return new SocialSituation
            {
                Type = "reflection",
                Confidence = 0.75,
                Signals = new[] { "reflection", "gratitude", "relationship maintenance" }
            };
        }

        if (ContainsAny(source,
                "personal challenges", "tough time", "health issues", "difficult",
                "appreciate sensitivity", "please be sensitive"))
        {
            return new SocialSituation
            {
                Type = "sensitive",
                Confidence = 0.80,
                Signals = new[] { "personal difficulty", "requires sensitivity", "restraint needed" }
            };
        }

        if (ContainsAny(source,
                "forbes", "30 under 30", "award", "recognition", "featured",
                "grateful for the journey", "proud to share"))
        {
            return new SocialSituation
            {
                Type = "achievement",
                Confidence = 0.85,
                Signals = new[] { "personal achievement", "recognition", "humblebrag potential" }
            };
        }

        if (ContainsAny(source,
                "merger", "acquisition", "breaking news", "announced today",
                "major development", "industry news"))
        {
            return new SocialSituation
            {
                Type = "news",
                Confidence = 0.80,
                Signals = new[] { "industry news", "breaking information", "comment opportunity" }
            };
        }

        if (ContainsAny(source,
                "job search", "open to opportunities", "looking for", "excited to announce"))
        {
            return new SocialSituation
            {
                Type = "job_search",
                Confidence = 0.85,
                Signals = new[] { "career transition", "job seeking", "mentoring opportunity" }
            };
        }

        if (ContainsAny(source,
                "team update", "implementing", "starting next", "process changes", "quarterly results", "financial update", "announcement", "launching a new product"))
        {
            return new SocialSituation
            {
                Type = "update",
                Confidence = 0.70,
                Signals = new[] { "team communication", "process change", "acknowledgment needed" }
            };
        }

        if (ContainsAny(source,
                "happy holidays", "holiday season", "joyful", "prosperous new year"))
        {
            return new SocialSituation
            {
                Type = "greeting",
                Confidence = 0.90,
                Signals = new[] { "seasonal greeting", "holiday wishes", "courtesy response" }
            };
        }

        if (ContainsAny(source,
                "good morning", "good afternoon", "good evening", "have a great day", "hope you're well", "hope this finds you well", "morning everyone", "evening everyone"))
        {
            return new SocialSituation
            {
                Type = "low_signal",
                Confidence = 0.85,
                Signals = new[] { "generic greeting", "low information", "no reply preferred" }
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
