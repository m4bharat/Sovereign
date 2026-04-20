using System.Linq;
using System.Text.RegularExpressions;
using Sovereign.Domain.Models;
using Sovereign.Intelligence.Interfaces;
using Sovereign.Intelligence.Models;

namespace Sovereign.Intelligence.Services;

public sealed class SocialSituationDetector : ISocialSituationDetector
{
    private SocialSituation DetectSpecificSituations(MessageContext context)
    {
        var source = (context.SourceText ?? string.Empty).Trim().ToLowerInvariant();

        // Priority 1: Defer/No-Reply (highest confidence, block others)
        if (ContainsAny(source, "reply later", "i'll reply later", "will reply later", "respond later", "reply when i", "more details soon"))
        {
            return new SocialSituation { Type = "defer_no_reply", Confidence = 0.95, Signals = new[] { "explicit defer" } };
        }

        if (ContainsAny(source, "controversial", "disagree", "hate", "worst", "terrible", "disaster", "fail") || 
            Regex.IsMatch(source, @"\b(woke|wokeism|pronouns|politic|election|trump|biden|climate hoax)\b"))
        {
            return new SocialSituation { Type = "controversial_no_reply", Confidence = 0.92, Signals = new[] { "high risk controversy" } };
        }

        // Priority 2: High-confidence specifics
        if (Regex.IsMatch(source, @"(?:promotion|milestone|new role|new chapter|grateful|gratitude|happy to share|thrilled|years at|anniversary)", RegexOptions.IgnoreCase))
        {
            return new SocialSituation { Type = "achievement_share", Confidence = 0.93, Signals = new[] { "career milestone" } };
        }

        if (Regex.IsMatch(source, @"(?:happy holidays|merry christmas|happy new year|season's greetings|joyful|prosperous|eid|diwali|hanukkah)", RegexOptions.IgnoreCase))
        {
            return new SocialSituation { Type = "holiday_greeting", Confidence = 0.95, Signals = new[] { "seasonal greeting" } };
        }

        if (ContainsAny(source, "team update", "group announcement", "we're hiring", "quarterly", "financial update", "product launch"))
        {
            return new SocialSituation { Type = "group_announcement", Confidence = 0.90, Signals = new[] { "org/team broadcast" } };
        }

        if (Regex.IsMatch(source, @"(?:#OpenToWork|#JobSearch|job search|open to work|open to opportunities|excited to announce new role)", RegexOptions.IgnoreCase))
        {
            return new SocialSituation { Type = "job_search", Confidence = 0.92, Signals = new[] { "job seeking" } };
        }

        if (ContainsAny(source, "funding", "raised", "series a", "series b", "investment", "backers", "thrilled to announce"))
        {
            return new SocialSituation { Type = "industry_news", Confidence = 0.88, Signals = new[] { "company milestone" } };
        }

        if (ContainsAny(source, "reflecting", "gratitude", "connections", "past year", "it's been"))
        {
            return new SocialSituation { Type = "relationship_preservation", Confidence = 0.85, Signals = new[] { "rm maintenance" } };
        }

        // Priority 3: CTA
        if (Regex.IsMatch(source, @"(?:drop in comments|comment below|share your|let me know|tell me|which one|what's your|your thoughts?|your take)", RegexOptions.IgnoreCase))
        {
            return new SocialSituation { Type = "cta_engagement", Confidence = 0.92, Signals = new[] { "explicit CTA" } };
        }

        // Priority 4: Personal update
        if (ContainsAny(source, "just wanted to share", "quick update", "thought you might like to know"))
        {
            return new SocialSituation { Type = "personal_update", Confidence = 0.75, Signals = new[] { "casual share" } };
        }

        return null;
    }

    public SocialSituation Detect(MessageContext context)
    {
        if (string.Equals(context.InteractionMode, "compose", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase))
        {
            var composeIntent =
                context.InteractionMetadata != null &&
                context.InteractionMetadata.TryGetValue("compose_intent", out var rawComposeIntent) &&
                bool.TryParse(rawComposeIntent, out var isComposeIntent) &&
                isComposeIntent;

            return new SocialSituation
            {
                Type = composeIntent ? "compose_post" : "compose_post",
                Summary = composeIntent
                    ? "The user provided a rough prompt for drafting a new post."
                    : "The user wants help composing a standalone post."
            };
        }

        if (string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(context.Message) &&
            context.Message.Trim().Length <= 30 &&
            !string.IsNullOrWhiteSpace(context.SourceText))
                {
                    return new SocialSituation
                    {
                        Type = "rewrite_feed_reply",
                        Summary = "The user provided a rough draft for a feed reply that should be rewritten into a specific comment."
                    };
                }

        if (string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase) &&
            context.InteractionMetadata != null &&
            context.InteractionMetadata.TryGetValue("rewrite_intent", out var rewriteIntent) &&
            bool.TryParse(rewriteIntent, out var isRewriteIntent) &&
            isRewriteIntent)
        {
            return new SocialSituation
            {
                Type = "rewrite_direct_message",
                Summary = "The user gave a rough chat intent that should be rewritten into a natural DM."
            };
        }

        if ((context.InteractionMode ?? string.Empty).Equals("chat", StringComparison.OrdinalIgnoreCase))
        {
            return new SocialSituation
            {
                Type = "direct_message",
                Summary = "This is a messaging/chat interaction that should sound natural and direct."
            };
        }

        var specific = DetectSpecificSituations(context);
        if (specific != null)
            return specific;

        var source = context.SourceText ?? string.Empty;

        if (ContainsAny(source,
                "joined", "joining", "new role", "new chapter", "excited to share",
                "grateful", "gratitude", "happy to share", "thrilled to share",
                "promotion", "milestone", "journey", "years at"))
        {
            return new SocialSituation
            {
                Type = "achievement_share",
                Confidence = 0.90,
                Signals = new[] { "career milestone", "gratitude", "new beginning", "isPublicCelebration" }
            };
        }

// hiring -> group_announcement (handled in specific)
        // Legacy hiring case deprecated

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

// team/org -> group_announcement (handled in specific)
        // Legacy update deprecated

// holiday -> handled in specific (holiday_greeting)
        // Legacy greeting deprecated

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
            Confidence = 0.40,
            Signals = new[] { "true generic fallback" }
        };
    }

    private static bool ContainsAny(string text, params string[] terms) =>
        terms.Any(term => Regex.IsMatch(text, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase));
}
