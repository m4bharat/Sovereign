using Sovereign.Domain.Models;
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
        var normalizedMessage = (context.Message ?? string.Empty).Trim();
        var hasUserDraft = !string.IsNullOrWhiteSpace(normalizedMessage);
        var sourceText = (context.SourceText ?? string.Empty).Trim();
        var situationType = (context.SituationType ?? string.Empty).Trim();

        return moveCandidates
            .Select(move =>
            {
                var reply = GenerateReplyForMove(move.Move, context, normalizedMessage, sourceText);

                // ===== GENERATION QUALITY FILTER =====
                if (!string.Equals(move.Move, "no_reply", StringComparison.OrdinalIgnoreCase))
                {
                    var trimmed = (reply ?? string.Empty).Trim();

                    // kill empty or near-empty replies early
                    if (trimmed.Length < 6)
                    {
                        reply = string.Empty;
                    }
                    else if (IsGenericReply(trimmed) && trimmed.Length < 60)
                    {
                        reply = BuildFallbackReply(move.Move, context, situationType);
                    }
                }

                var shortReply = BuildShortReplyForMove(move.Move, context, normalizedMessage, sourceText);

                System.Diagnostics.Debug.WriteLine(
                    $"[Sovereign][Generator] Move={move.Move}, Situation={situationType}, Surface={context.Surface}, Reply={reply}");

                return new SocialMoveCandidate
                {
                    Move = move.Move,
                    Rationale = move.Rationale,
                    Reply = reply,
                    ShortReply = shortReply,
                    GenerationConfidence = EstimateConfidence(move.Move, reply, hasUserDraft, sourceText),
                    RequiresPolish = ShouldPolish(reply),
                };
            })
            .ToArray();
    }

    private static string GenerateReplyForMove(
        string move,
        MessageContext context,
        string userDraft,
        string sourceText)
    {
        var isFeedReply = string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase);
        var isCompose = string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase);
        var hasDraft = !string.IsNullOrWhiteSpace(userDraft);
        var author = context.SourceAuthor?.Trim();
        var topic = ExtractTopic(sourceText);
        var situationType = (context.SituationType ?? string.Empty).Trim().ToLowerInvariant();

        return move switch
        {
            "rewrite_user_intent" =>
                RewriteUserDraft(userDraft, sourceText, isFeedReply, isCompose, author),

            "draft_post" =>
                GeneratePost(userDraft),

            "outline_post" =>
                GeneratePostOutline(userDraft),

            "add_specific_insight" =>
                GenerateInsightReply(sourceText, userDraft),

            "add_insight" =>
                GenerateInsightReply(sourceText, userDraft),

            "light_touch" =>
                GenerateLightReply(sourceText, userDraft),

            "light_touch_question" =>
                GenerateLightTouchQuestion(sourceText),

            "answer_question" =>
                GenerateHelpfulReply(userDraft, sourceText),

            "respond_helpfully" =>
                GenerateHelpfulReply(userDraft, sourceText),

            "congratulate" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Congratulations on this milestone. Wishing you the very best ahead."
                    : $"Congratulations, {author}. Wishing you the very best ahead.",

            "congratulate_encourage" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Congratulations on this milestone — exciting to see the momentum behind your journey."
                    : $"Congratulations, {author} — exciting to see the momentum behind your journey.",

            "appreciate_journey" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Really appreciate the journey behind this — it clearly reflects consistency and effort."
                    : $"Really appreciate the journey behind this, {author} — it clearly reflects consistency and effort.",

            "express_interest" =>
                hasDraft
                    ? RewriteStandaloneSentence(userDraft)
                    : "This looks like an interesting opportunity. I’d love to learn more.",

            "amplify_signal" =>
                "This is worth getting in front of the right people — strong opportunity.",

            "offer_support" =>
                "Happy to support or help connect the dots if that would be useful.",

            "agree" =>
                hasDraft
                    ? RewriteStandaloneSentence(userDraft)
                    : "I agree with the core point here — the execution layer is where the real difference shows up.",

            "add_nuance" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "I agree — and I’d add that the real difference usually comes from how quickly these ideas get operationalized."
                    : $"I agree — and I’d add that with {topic}, the real difference usually comes from how quickly it gets operationalized.",

            "answer_supportively" =>
                RequiresCtaPositioning(context)
                    ? BuildCtaParticipationReply(context)
                    : GenerateHelpfulReply(userDraft, sourceText),

            "acknowledge_update" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "Thanks for the update — good to see the progress."
                    : $"Thanks for the update on {topic} — good to see the progress.",

            "encourage" =>
                hasDraft
                    ? RewriteStandaloneSentence(userDraft)
                    : "Strong direction — looking forward to seeing how this develops.",

            "appreciate" =>
                hasDraft && (isFeedReply || isCompose)
                    ? RewriteUserDraft(userDraft, sourceText, isFeedReply, isCompose, author)
                    : string.IsNullOrWhiteSpace(topic)
                        ? "Really clear and useful framing."
                        : $"Really clear framing on {topic} — easy to connect to real-world execution.",

            "ask_relevant_question" =>
                BuildFramedQuestion(context, move),

            "defer" =>
                "Impressive achievement. Well deserved.",

            "direct_message" =>
                "This feels worth continuing in DM.",

            "ask_details" =>
                "Could you share a bit more detail on how you’re thinking about this?",

            "engage_privately" =>
                "This feels like a conversation worth continuing privately — happy to discuss further.",

            "engage" =>
                string.IsNullOrWhiteSpace(topic)
                    ? "This resonates — there’s a lot to unpack in how this plays out in practice."
                    : $"This resonates — especially how {topic} plays out in practice.",

            "praise" =>
                "Strong work — this is a meaningful result.",

            "respond" =>
                "Thanks for sharing this.",

            "acknowledge" =>
                hasDraft
                    ? RewriteStandaloneSentence(userDraft)
                    : "Thanks for the update.",

            "no_reply" =>
                string.Empty,

            _ =>
                DefaultReply(context, userDraft, sourceText, situationType, hasDraft)
        };
    }

    private static string BuildShortReplyForMove(
        string move,
        MessageContext context,
        string userDraft,
        string sourceText)
    {
        var author = context.SourceAuthor?.Trim();

        return move switch
        {
            "congratulate" =>
                string.IsNullOrWhiteSpace(author)
                    ? "Congrats on the milestone!"
                    : $"Congrats, {author}!",

            "appreciate" =>
                "Clear and useful perspective.",

            "ask_relevant_question" =>
                "Curious — how does this play out in practice?",

            "rewrite_user_intent" =>
                RewriteStandaloneSentence(userDraft),

            "draft_post" =>
                (userDraft ?? string.Empty).Trim(),

            "light_touch" =>
                "Interesting direction — especially at scale.",

            _ =>
                "Thoughtful perspective."
        };
    }

    private static string RewriteUserDraft(
        string draft,
        string sourceText,
        bool isFeedReply,
        bool isCompose,
        string? author)
    {
        if (string.IsNullOrWhiteSpace(draft))
            return string.Empty;

        var cleanDraft = RewriteStandaloneSentence(draft);
        if (cleanDraft.Length < 25)
        {
            cleanDraft = $"{cleanDraft} — this highlights a broader shift in how these systems are actually being deployed and scaled.";
        }

        var topic = ExtractTopic(sourceText);

        if (isCompose)
        {
            return $"{cleanDraft}\n\nWe’re moving into a phase where the real differentiator is no longer just experimentation — it’s deployment at scale. The advantage will go to the teams and ecosystems that can operationalize these shifts faster than everyone else.";
        }

        if (isFeedReply && !string.IsNullOrWhiteSpace(sourceText))
        {
            if (!string.IsNullOrWhiteSpace(topic))
            {
                return $"{cleanDraft} — it really shows how {topic} is becoming less about isolated innovation and more about scale, execution, and deployment speed.";
            }

            return $"{cleanDraft} — it really highlights how scale and execution are becoming the real differentiators here.";
        }

        if (!string.IsNullOrWhiteSpace(author))
        {
            return $"{cleanDraft}, {author}.";
        }

        return cleanDraft;
    }

    private static string GenerateInsightReply(string sourceText, string userDraft)
    {
        if (!string.IsNullOrWhiteSpace(userDraft))
        {
            var rewritten = RewriteStandaloneSentence(userDraft);
            if (!string.IsNullOrWhiteSpace(sourceText))
            {
                return $"{rewritten} — what stands out is how quickly the value compounds once deployment starts happening at scale.";
            }

            return rewritten;
        }

        if (string.IsNullOrWhiteSpace(sourceText))
            return "What stands out here is how much the real advantage comes from execution, not just the idea itself.";

        var topic = ExtractTopic(sourceText);

        if (!string.IsNullOrWhiteSpace(topic))
        {
            return $"What stands out here is that {topic} is no longer just about the technology itself — the real edge comes from how fast it can be deployed and operationalized.";
        }

        return "What stands out here is not just the technology itself, but how quickly it is being deployed at scale — that is where the real competitive advantage begins to emerge in practice.";
    }

    private static string GenerateLightReply(string sourceText, string userDraft)
    {
        if (!string.IsNullOrWhiteSpace(userDraft))
        {
            return RewriteStandaloneSentence(userDraft);
        }

        if (!string.IsNullOrWhiteSpace(sourceText))
        {
            return "Really interesting direction — the pace of real-world adoption here is what makes it especially impactful.";
        }

        return "Interesting direction — especially in how this can scale in practice.";
    }

    private static string GenerateLightTouchQuestion(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
            return "Curious — where do you see this heading next?";

        return "Where do you see the biggest real-world impact showing up first?";
    }

    private static string GenerateHelpfulReply(string draft, string sourceText)
    {
        if (!string.IsNullOrWhiteSpace(draft))
        {
            var rewritten = RewriteStandaloneSentence(draft);

            if (!string.IsNullOrWhiteSpace(sourceText))
            {
                return $"{rewritten} — one thing that stands out is how much the real impact will depend on speed of adoption and execution.";
            }

            return rewritten;
        }

        if (!string.IsNullOrWhiteSpace(sourceText))
        {
            return "One thing that stands out here is how quickly the impact compounds once adoption starts happening at scale.";
        }

        return "One thing to consider is how this evolves once it moves from idea to large-scale execution.";
    }

    private static string GeneratePost(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return string.Empty;

        var normalized = RewriteStandaloneSentence(topic);

        return $"{normalized}\n\nWe are entering a phase where execution speed matters more than ideas alone. The real gap is no longer between those who understand the technology and those who do not — it is between those who can operationalize it at scale and those who cannot.\n\nThe next wave of advantage will come from deployment discipline, not just technical capability.";
    }

    private static string GeneratePostOutline(string topic)
    {
        if (string.IsNullOrWhiteSpace(topic))
            return string.Empty;

        var normalized = RewriteStandaloneSentence(topic);

        return $"{normalized}\n\n1. What is changing right now\n2. Why this matters beyond the surface trend\n3. Where the real competitive advantage will come from\n4. What this means going forward";
    }

    private static string DefaultReply(
        MessageContext context,
        string userDraft,
        string sourceText,
        string situationType,
        bool hasDraft)
    {
        if (hasDraft)
        {
            return RewriteUserDraft(
                userDraft,
                sourceText,
                string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase),
                string.Equals(context.Surface, "start_post", StringComparison.OrdinalIgnoreCase),
                context.SourceAuthor);
        }

        return situationType switch
        {
            "rewrite_feed_reply" => GenerateInsightReply(sourceText, userDraft),
            "compose_post" => GeneratePost(context.Message ?? string.Empty),
            "rewrite_direct_message" => RewriteStandaloneSentence(userDraft),
            _ => GenerateFallbackReply(sourceText)
        };
    }

    private static string GenerateFallbackReply(string sourceText)
    {
        if (string.IsNullOrWhiteSpace(sourceText))
            return "Appreciate you sharing this.";

        var lower = sourceText.ToLowerInvariant();

        if (lower.Contains("award") ||
            lower.Contains("recognition") ||
            lower.Contains("milestone") ||
            lower.Contains("employee of the quarter") ||
            lower.Contains("congratulations"))
        {
            return "Congratulations on this well-deserved recognition.";
        }

        if (lower.Contains("?"))
        {
            return "Interesting question — there’s a lot in how this plays out in practice.";
        }

        return "Appreciate you sharing this.";
    }

    private static string BuildFallbackReply(
        string move,
        MessageContext context,
        string situationType)
    {
        var author = context.SourceAuthor?.Trim();
        var sourceText = context.SourceText ?? string.Empty;
        var lower = sourceText.ToLowerInvariant();

        if (string.Equals(context.InteractionMode, "chat", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(situationType, "direct_message", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(situationType, "rewrite_direct_message", StringComparison.OrdinalIgnoreCase))
        {
            if (lower.Contains("birthday"))
            {
                return string.IsNullOrWhiteSpace(author)
                    ? "Thanks so much for the birthday wishes! I really appreciate it."
                    : $"Thanks so much for the birthday wishes, {author}! I really appreciate it.";
            }

            return string.IsNullOrWhiteSpace(author)
                ? "Thanks so much — I really appreciate it."
                : $"Thanks so much, {author} — I really appreciate it.";
        }

        if (string.Equals(context.Surface, "feed_reply", StringComparison.OrdinalIgnoreCase))
        {
            if (lower.Contains("award") ||
                lower.Contains("recognition") ||
                lower.Contains("employee of the quarter") ||
                lower.Contains("milestone") ||
                lower.Contains("congratulations"))
            {
                return "Congratulations on this well-deserved recognition.";
            }

            if (lower.Contains("?"))
            {
                return "Really interesting question — I think a lot depends on how this plays out in practice.";
            }

            return "Appreciate you sharing this.";
        }

        return move switch
        {
            "congratulate" => "Congratulations on this well-deserved milestone.",
            "congratulate_encourage" => "Congratulations on this milestone. Wishing you continued success ahead.",
            "appreciate_journey" => "The journey behind this really comes through clearly.",
            "express_interest" => "This looks genuinely interesting. I'd love to learn more.",
            "answer_supportively" => "That's a thoughtful question. A lot depends on how it plays out in practice.",
            "ask_relevant_question" => "What has been the biggest learning from this so far?",
            _ => "Appreciate you sharing this."
        };
    }

    private static bool IsGenericReply(string reply)
    {
        var text = reply.Trim().ToLowerInvariant();

        var forbiddenPhrases = new[]
        {
            "great post", "well said", "thanks for sharing", "interesting perspective", "nice breakdown",
            "good point", "so true", "very insightful", "thoughtful perspective", "interesting direction",
            "clear framing", "useful framing", "great perspective", "well put", "spot on",
            "deployment speed", "operationalize", "scale", "execution layer" // task banned generics
        };

        return forbiddenPhrases.Any(text.Contains);
    }

    private static bool ShouldPolish(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
            return false;

        return reply.Length > 80;
    }

    private static double EstimateConfidence(string move, string reply, bool hasDraft, string sourceText)
    {
        if (string.Equals(move, "no_reply", StringComparison.OrdinalIgnoreCase))
            return 0.50;

        if (string.IsNullOrWhiteSpace(reply))
            return 0.25;

        if ((string.Equals(move, "rewrite_user_intent", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(move, "draft_post", StringComparison.OrdinalIgnoreCase)) && hasDraft)
            return 0.92;

        if (!string.IsNullOrWhiteSpace(sourceText) && reply.Length > 40)
            return 0.88;

        return 0.80;
    }

    private static string RewriteStandaloneSentence(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var cleaned = Regex.Replace(text.Trim(), "\\s+", " ");

        cleaned = cleaned.Replace("Im ", "I'm ", StringComparison.OrdinalIgnoreCase);
        cleaned = cleaned.Replace(" i ", " I ", StringComparison.Ordinal);
        cleaned = cleaned.Trim();

        if (cleaned.Length == 0)
            return string.Empty;

        cleaned = char.ToUpperInvariant(cleaned[0]) + cleaned[1..];

        if (!cleaned.EndsWith(".") &&
            !cleaned.EndsWith("!") &&
            !cleaned.EndsWith("?"))
        {
            cleaned += ".";
        }

        return cleaned;
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

    private static string BuildFramedQuestion(MessageContext context, string move)
    {
        var framing = GenerateFramingLine(context);
        var question = GenerateQuestionLine(context, move);

        if (string.IsNullOrWhiteSpace(framing))
            return question;

        return $"{framing}\n\n{question}";
    }

    private static string GenerateFramingLine(MessageContext context)
    {
        var source = $"{context.SourceTitle ?? string.Empty} {context.SourceText ?? string.Empty}".ToLowerInvariant();

        if (context.SituationType == "recruitment" || source.Contains("graduates") || source.Contains("career changers"))
            return "Programs like this do a good job closing the gap between learning and real-world delivery.";

        if (context.SituationType == "educational")
            return "The strongest part of programs like this is usually the transition from theory into live execution.";

        if (context.SituationType == "opinion")
            return "That's a useful framing—especially where execution depends on more than the surface idea.";

        if (context.SituationType == "milestone")
            return "What stands out here is how much structured exposure can accelerate real growth early on.";

        return string.Empty;
    }

    private static string GenerateQuestionLine(MessageContext context, string move)
    {
        var angle = InferQuestionAngle(context);

        return angle switch
        {
            "transition" => "I'm curious—what tends to be the hardest part when people move from structured learning into actual client work?",
            "constraint" => "I'm curious—where do graduates or career-changers usually hit the biggest execution constraints once they join live projects?",
            "pattern" => "I'm curious—what pattern shows up most often in the people who adapt quickly versus those who take longer?",
            "tradeoff" => "I'm curious—what's the hardest balance to get right between learning support and real project expectations?",
            "selection" => "I'm curious—what tends to separate the people who ramp successfully from those who struggle early on?",
            _ => "I'm curious—what tends to be the hardest part when people enter real-world delivery for the first time?"
        };
    }

    private static string InferQuestionAngle(MessageContext context)
    {
        var source = $"{context.SourceTitle ?? string.Empty} {context.SourceText ?? string.Empty}".ToLowerInvariant();

        if (source.Contains("client") || source.Contains("real-world") || source.Contains("actual client work"))
            return "transition";

        if (source.Contains("mentorship") || source.Contains("structured learning"))
            return "tradeoff";

        if (source.Contains("graduates") || source.Contains("career changers"))
            return "selection";

        if (source.Contains("ai") || source.Contains("responsibly") || source.Contains("pairing"))
            return "constraint";

        return "pattern";
    }

    private static bool IsCtaEngagementPost(MessageContext context)
    {
        var source = string.Join(" ",
            context.SourceTitle ?? string.Empty,
            context.SourceText ?? string.Empty,
            context.ParentContextText ?? string.Empty,
            context.NearbyContextText ?? string.Empty)
            .ToLowerInvariant();

        var signals = new[]
        {
            "drop in the comments",
            "comment below",
            "let me know",
            "tell me",
            "where are you right now",
            "which skill",
            "what are you learning next",
            "what are you working on",
            "share in the comments",
            "comment your",
            "reply with",
            "what's your next step"
        };

        return signals.Any(signal => source.Contains(signal));
    }

    private static bool RequiresCtaPositioning(MessageContext context)
    {
        return IsCtaEngagementPost(context);
    }

    private static string BuildCtaParticipationReply(MessageContext context)
    {
        var role = InferCtaRole(context);
        var skill = InferCtaSkill(context);
        var concept = InferCtaConcept(skill);
        var skillText = string.IsNullOrWhiteSpace(skill) ? "the next area I'm focusing on" : skill;

        return $"I'm currently coming from the {role} side, and {skillText} is the next area I'm focusing on. It feels like that's where the shift moves from theory into {concept}.";
    }

    private static string InferCtaRole(MessageContext context)
    {
        var source = $"{context.SourceTitle ?? string.Empty} {context.SourceText ?? string.Empty} {context.ParentContextText ?? string.Empty} {context.NearbyContextText ?? string.Empty}".ToLowerInvariant();

        if (source.Contains("sysadmin"))
            return "sysadmin";
        if (source.Contains("engineer"))
            return "engineering";
        if (source.Contains("developer"))
            return "developer";
        if (source.Contains("manager"))
            return "manager";

        return "developer";
    }

    private static string InferCtaSkill(MessageContext context)
    {
        var source = $"{context.SourceTitle ?? string.Empty} {context.SourceText ?? string.Empty} {context.ParentContextText ?? string.Empty} {context.NearbyContextText ?? string.Empty}".ToLowerInvariant();
        var skills = new[] { "kubernetes", "terraform", "docker", "observability", "sre", "security", "ci/cd", "platform" };

        foreach (var skill in skills)
        {
            if (source.Contains(skill))
                return skill;
        }

        return string.Empty;
    }

    private static string InferCtaConcept(string skill)
    {
        var s = (skill ?? string.Empty).ToLowerInvariant();

        if (s.Contains("kubernetes"))
            return "orchestration and reliability";
        if (s.Contains("terraform"))
            return "infrastructure thinking at scale";
        if (s.Contains("docker"))
            return "repeatable deployment patterns";
        if (s.Contains("observability"))
            return "system visibility and operational feedback";
        if (s.Contains("sre"))
            return "reliability and production discipline";

        return "real-world delivery";
    }
}