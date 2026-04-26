(() => {
    const DEBUG = true;
    const BUTTON_CLASS = "sovereign-linkedin-button";
    const STATUS_CLASS = "sovereign-linkedin-status";
    const SLOT_CLASS = "sovereign-action-slot";
    const AUTH_KEY = "sovereignAuthToken";
    const PENDING_KEY = "sovereignPendingSuggestion";
    const STYLE_ID = "sovereign-inline-styles";

    let scanTimer = null;
    let observerStarted = false;
    let lastFocusedComposer = null;

    function getPlatformConfig() {
        const host = window.location.hostname.toLowerCase();
        const isLinkedIn = host === "www.linkedin.com" || host.endsWith(".linkedin.com");
        const isX = host === "x.com" || host === "www.x.com" || host === "x.xom" || host === "www.x.xom";

        if (isX) {
            return {
                key: "x",
                label: "X",
                contactPrefix: "x"
            };
        }

        return {
            key: isLinkedIn ? "linkedin" : "social",
            label: isLinkedIn ? "LinkedIn" : "Social",
            contactPrefix: isLinkedIn ? "linkedin" : "social"
        };
    }

    function log(...args) {
        if (DEBUG) {
            console.log("[Sovereign]", ...args);
        }
    }

    function isElement(node) {
        return !!node && node.nodeType === Node.ELEMENT_NODE;
    }

    function isVisible(node) {
        if (!node || !isElement(node)) return false;

        const style = window.getComputedStyle(node);
        if (style.display === "none" || style.visibility === "hidden") return false;

        return !!(node.offsetParent || node.getClientRects().length);
    }

    function ensureStyles() {
        if (document.getElementById(STYLE_ID)) return;

        const style = document.createElement("style");
        style.id = STYLE_ID;
        style.textContent = `
            .${SLOT_CLASS} {
                display: flex !important;
                align-items: center !important;
                gap: 8px !important;
                margin: 8px 0 !important;
                padding: 4px 0 !important;
                flex-wrap: wrap !important;
                position: relative !important;
                z-index: 999999 !important;
            }

            .${BUTTON_CLASS} {
                background: #0a66c2 !important;
                color: #fff !important;
                border: none !important;
                border-radius: 999px !important;
                padding: 6px 12px !important;
                cursor: pointer !important;
                font-weight: 600 !important;
                font-size: 12px !important;
                line-height: 1.2 !important;
                white-space: nowrap !important;
                margin: 0 !important;
                z-index: 999999 !important;
                box-shadow: none !important;
                appearance: none !important;
            }

            .${BUTTON_CLASS}:hover {
                background: #004182 !important;
            }

            .${BUTTON_CLASS}[data-sovereign-busy="true"] {
                opacity: 0.72 !important;
                cursor: wait !important;
            }

            .${STATUS_CLASS} {
                font-size: 12px !important;
                line-height: 1.3 !important;
                color: #666 !important;
                max-width: 420px !important;
                word-break: break-word !important;
            }

            .${STATUS_CLASS}[data-type="error"] {
                color: #b00020 !important;
            }

            .${STATUS_CLASS}[data-type="success"] {
                color: #1a7f37 !important;
            }
        `;
        document.head.appendChild(style);
    }

    function debounceScan() {
        if (scanTimer) clearTimeout(scanTimer);
        scanTimer = setTimeout(() => {
            scanForComposers();
            tryInjectFromActiveElement();
        }, 250);
    }

    function closestSafe(el, selector) {
        try {
            return el?.closest?.(selector) || null;
        } catch {
            return null;
        }
    }

    function dedupe(items) {
        return Array.from(new Set(items.filter(Boolean)));
    }

    function normalizeText(text, maxLen = 3500) {
        return (text || "")
            .replace(/\u200B/g, "")
            .replace(/\s+/g, " ")
            .trim()
            .slice(0, maxLen);
    }

    function isEditableTextbox(el) {
        if (!el || !isElement(el)) return false;

        const isContentEditable = el.getAttribute("contenteditable") === "true";
        const isProseMirror = el.matches?.(".ProseMirror, .tiptap.ProseMirror");
        const dataTestId = (el.getAttribute("data-testid") || "").toLowerCase();

        if (!isContentEditable && !isProseMirror && !dataTestId.includes("tweettextarea")) return false;

        const role = el.getAttribute("role") || "";
        const ariaMultiline = el.getAttribute("aria-multiline") || "";
        const ariaLabel = (el.getAttribute("aria-label") || "").toLowerCase();
        const placeholder = (
            el.getAttribute("aria-placeholder") ||
            el.getAttribute("data-placeholder") ||
            ""
        ).toLowerCase();

        return (
            isProseMirror ||
            role === "textbox" ||
            ariaMultiline === "true" ||
            dataTestId.includes("tweettextarea") ||
            ariaLabel.includes("post") ||
            ariaLabel.includes("reply") ||
            ariaLabel.includes("tweet") ||
            ariaLabel.includes("write") ||
            ariaLabel.includes("message") ||
            ariaLabel.includes("text editor") ||
            placeholder.includes("post") ||
            placeholder.includes("reply") ||
            placeholder.includes("tweet") ||
            placeholder.includes("write") ||
            placeholder.includes("message") ||
            placeholder.includes("talk about")
        );
    }

    function detectLinkedInSurface(composer) {
        if (!composer) return { surface: "unknown", container: null };

        const shareBox =
            closestSafe(composer, ".share-box") ||
            closestSafe(composer, ".share-box-feed-entry__trigger") ||
            closestSafe(composer, ".share-creation-state");

        if (shareBox) {
            return { surface: "start_post", container: shareBox };
        }

        const msgForm = closestSafe(composer, ".msg-form");
        if (msgForm) {
            return { surface: "messaging_chat", container: msgForm };
        }

        const msgBubble = closestSafe(composer, ".msg-overlay-conversation-bubble");
        if (msgBubble) {
            return { surface: "messaging_chat", container: msgBubble };
        }

        const feedContainer =
            closestSafe(composer, '[role="listitem"]') ||
            closestSafe(composer, ".feed-shared-update-v2") ||
            closestSafe(composer, "article");

        if (feedContainer) {
            return { surface: "feed_reply", container: feedContainer };
        }

        return {
            surface: "unknown",
            container: composer.parentElement || null
        };
    }

    function detectXSurface(composer) {
        if (!composer) return { surface: "unknown", container: null };

        const dmContainer =
            closestSafe(composer, '[data-testid="dmComposerTextInput"]') ||
            closestSafe(composer, '[data-testid="DmComposerTextInput"]') ||
            closestSafe(composer, '[data-testid="DMDrawer"]') ||
            (window.location.pathname.startsWith("/messages") ? closestSafe(composer, "main") : null);

        if (dmContainer) {
            return { surface: "messaging_chat", container: dmContainer };
        }

        const tweetContainer =
            closestSafe(composer, '[data-testid="tweet"]') ||
            closestSafe(composer, 'article[data-testid="tweet"]') ||
            closestSafe(composer, "article");

        if (tweetContainer) {
            return { surface: "feed_reply", container: tweetContainer };
        }

        const form = closestSafe(composer, "form");
        if (form) {
            const buttonText = normalizeText(form.innerText || "", 400).toLowerCase();
            if (buttonText.includes("reply")) {
                return { surface: "feed_reply", container: form };
            }

            return { surface: "start_post", container: form };
        }

        if (
            window.location.pathname.startsWith("/compose/post") ||
            window.location.pathname === "/home"
        ) {
            return { surface: "start_post", container: composer.parentElement || null };
        }

        return {
            surface: "unknown",
            container: composer.parentElement || null
        };
    }

    function detectSurface(composer) {
        const platform = getPlatformConfig();
        if (platform.key === "x") {
            return detectXSurface(composer);
        }

        return detectLinkedInSurface(composer);
    }

    function getInjectionHost(composer) {
        const { surface, container } = detectSurface(composer);
        const platform = getPlatformConfig();

        if (platform.key === "x") {
            if (surface === "messaging_chat") {
                return (
                    closestSafe(composer, "form") ||
                    container?.querySelector?.('[data-testid="dmComposerSendButton"]')?.parentElement ||
                    composer.parentElement ||
                    container
                );
            }

            if (surface === "start_post" || surface === "feed_reply") {
                return closestSafe(composer, "form") || composer.parentElement || container;
            }

            return composer.parentElement || container;
        }

        if (surface === "start_post") {
            return (
                container?.querySelector(".share-creation-state__footer") ||
                container?.querySelector(".share-creation-state__additional-toolbar") ||
                container?.querySelector(".share-actions") ||
                container?.querySelector(".share-creation-state") ||
                composer.parentElement ||
                container
            );
        }

        if (surface === "messaging_chat") {
            const msgForm = closestSafe(composer, ".msg-form") || container;
            return (
                msgForm?.querySelector(".msg-form__footer") ||
                msgForm?.querySelector(".msg-form__msg-content-container") ||
                msgForm?.querySelector(".msg-form__contenteditable-container") ||
                composer.parentElement ||
                msgForm
            );
        }

        if (surface === "feed_reply") {
            return (
                closestSafe(composer, ".comments-comment-box__form-container") ||
                closestSafe(composer, "form") ||
                composer.parentElement ||
                container
            );
        }

        return composer.parentElement || container;
    }

    function getOrCreateActionSlot(host, surface) {
        if (!host) return null;

        let slot = host.querySelector(`:scope > .${SLOT_CLASS}[data-surface="${surface}"]`);
        if (slot) return slot;

        slot = document.createElement("div");
        slot.className = SLOT_CLASS;
        slot.setAttribute("data-surface", surface);

        if (surface === "messaging_chat") {
            host.appendChild(slot);
        } else if (surface === "start_post") {
            host.appendChild(slot);
        } else {
            host.prepend(slot);
        }

        return slot;
    }

    function getOrCreateStatus(slot) {
        let status = slot.querySelector(`.${STATUS_CLASS}`);
        if (status) return status;

        status = document.createElement("div");
        status.className = STATUS_CLASS;
        status.setAttribute("data-type", "info");
        slot.appendChild(status);
        return status;
    }

    function setStatus(slot, text, type = "info") {
        const status = getOrCreateStatus(slot);
        status.textContent = text || "";
        status.setAttribute("data-type", type);
    }

    function clearStatus(slot) {
        const status = slot.querySelector(`.${STATUS_CLASS}`);
        if (status) {
            status.textContent = "";
            status.setAttribute("data-type", "info");
        }
    }

    function getComposerText(composer) {
        return normalizeText(composer.innerText || composer.textContent || "", 3000);
    }

    function getSourceAuthor(container) {
        if (!container) return "";

        const platform = getPlatformConfig();
        if (platform.key === "x") {
            const candidates = container.querySelectorAll(
                '[data-testid="User-Name"] span, a[href^="/"][role="link"] span'
            );

            for (const node of candidates) {
                const text = normalizeText(node.innerText || "", 120);
                if (text && !text.startsWith("@")) {
                    return text;
                }
            }

            return "";
        }

        const candidates = container.querySelectorAll('a[href*="/in/"], a[href*="/company/"]');
        for (const link of candidates) {
            const text = normalizeText(link.innerText || "", 120);
            if (text && !/follow|suggested/i.test(text) && text.length < 120) {
                return text;
            }
        }

        return "";
    }

    function getTextFromSelectors(root, selectors, maxLen = 3500) {
        if (!root) return "";

        for (const selector of selectors) {
            const el = root.querySelector(selector);
            const text = normalizeText(el?.innerText || "", maxLen);
            if (text) return text;
        }

        return "";
    }

    function getSourceText(container) {
        if (!container) return "";

        const platform = getPlatformConfig();
        if (platform.key === "x") {
            return getTextFromSelectors(
                container,
                [
                    '[data-testid="tweetText"]',
                    '[data-testid="tweetTextarea_0"]',
                    '[role="textbox"]'
                ],
                3500
            );
        }

        return getTextFromSelectors(
            container,
            [
                '[data-testid="expandable-text-box"]',
                ".feed-shared-inline-show-more-text",
                ".feed-shared-update-v2__description",
                ".update-components-text",
                ".update-components-text-view",
                ".break-words"
            ],
            3500
        );
    }

    function getSourceTitle(container) {
        if (!container) return "";

        const platform = getPlatformConfig();
        if (platform.key === "x") {
            const title =
                getTextFromSelectors(
                    container,
                    [
                        '[data-testid="User-Name"]',
                        'a[href^="/"][role="link"]'
                    ],
                    200
                ) || document.title;

            return normalizeText(title, 200);
        }

        const nodes = container.querySelectorAll("p, span");
        for (const node of nodes) {
            const text = normalizeText(node.innerText || "", 300);
            if (!text) continue;
            if (/follow|suggested/i.test(text)) continue;
            if (text.length > 30 || text.includes("|")) return text;
        }

        return "";
    }

    function getNearbyComments(container) {
        if (!container) return "";

        const platform = getPlatformConfig();
        if (platform.key === "x") {
            const nodes = container.parentElement?.querySelectorAll?.('[data-testid="tweetText"]') || [];
            const out = [];
            const seen = new Set();

            for (const node of nodes) {
                const text = normalizeText(node.innerText || "", 300);
                if (!text || seen.has(text)) continue;
                seen.add(text);
                out.push(text);
                if (out.length >= 4) {
                    break;
                }
            }

            return out.join("\n").slice(0, 1200);
        }

        const selectors = [
            ".comments-comment-item__main-content",
            ".comments-comment-item-content-body",
            ".comments-comment-item [dir='ltr']",
            ".comments-comments-list__comment-item span[dir='ltr']"
        ];

        const seen = new Set();
        const out = [];

        for (const selector of selectors) {
            const nodes = container.querySelectorAll(selector);
            for (const node of nodes) {
                const text = normalizeText(node.innerText || "", 300);
                if (!text) continue;
                if (seen.has(text)) continue;
                seen.add(text);
                out.push(text);
                if (out.length >= 4) {
                    return out.join("\n").slice(0, 1200);
                }
            }
        }

        return out.join("\n").slice(0, 1200);
    }

    function getMessageRecipientName(composer) {
        const platform = getPlatformConfig();
        if (platform.key === "x") {
            const panel =
                closestSafe(composer, '[data-testid="DMDrawer"]') ||
                closestSafe(composer, "main");

            const title =
                panel?.querySelector?.('[data-testid="DMDrawer"] [dir="auto"]') ||
                panel?.querySelector?.('header [dir="auto"]');

            return normalizeText(title?.innerText || "", 200) || "x-message-contact";
        }

        const bubble =
            closestSafe(composer, ".msg-overlay-conversation-bubble") ||
            closestSafe(composer, ".msg-form") ||
            closestSafe(composer, ".msg-thread");

        const title =
            bubble?.querySelector(".msg-overlay-bubble-header__title span") ||
            bubble?.querySelector(".msg-overlay-bubble-header__title") ||
            bubble?.querySelector(".msg-thread__link-to-profile") ||
            bubble?.querySelector(".msg-entity-lockup__entity-title");

        return normalizeText(title?.innerText || "", 200) || "linkedin-message-contact";
    }

    function getLatestMessageContext(composer) {
        const platform = getPlatformConfig();
        if (platform.key === "x") {
            const panel =
                closestSafe(composer, '[data-testid="DMDrawer"]') ||
                closestSafe(composer, "main");

            if (!panel) {
                return {
                    latestMessage: "",
                    nearbyMessages: "",
                    recentRelationshipSummary: ""
                };
            }

            const messageItems = Array.from(
                panel.querySelectorAll('[data-testid="messageEntry"], [data-testid="conversation"]')
            );

            const visibleMessages = messageItems
                .map((item) => {
                    const body = normalizeText(item.innerText || "", 280);
                    if (!body) return null;

                    return {
                        sender: "",
                        body,
                        combined: body
                    };
                })
                .filter(Boolean);

            const latestMessage = visibleMessages.length
                ? visibleMessages[visibleMessages.length - 1].combined
                : "";

            const nearbyMessages = visibleMessages
                .slice(-5)
                .map((m) => m.combined)
                .join("\n");

            const recentRelationshipSummary = visibleMessages
                .slice(-3)
                .map((m) => m.combined)
                .join(" | ");

            return {
                latestMessage,
                nearbyMessages,
                recentRelationshipSummary
            };
        }

        const bubble =
            closestSafe(composer, ".msg-overlay-conversation-bubble") ||
            closestSafe(composer, ".msg-form") ||
            closestSafe(composer, ".msg-thread") ||
            closestSafe(composer, ".msg-convo-wrapper");

        if (!bubble) {
            return {
                latestMessage: "",
                nearbyMessages: "",
                recentRelationshipSummary: ""
            };
        }

        const messageItems = Array.from(
            bubble.querySelectorAll(
                ".msg-s-event-listitem, .msg-s-message-list__event, .msg-s-message-group"
            )
        );

        const visibleMessages = messageItems
            .map((item) => {
                const sender =
                    normalizeText(
                        item.querySelector(".msg-s-message-group__name")?.innerText ||
                        item.querySelector(".msg-s-message-group__profile-link")?.innerText ||
                        "",
                        120
                    ) || "";

                const body =
                    normalizeText(
                        item.querySelector(".msg-s-event-listitem__body")?.innerText ||
                        item.querySelector(".msg-s-message-group__message-bubble")?.innerText ||
                        item.querySelector("p")?.innerText ||
                        "",
                        280
                    ) || "";

                if (!body) return null;

                return {
                    sender,
                    body,
                    combined: sender ? `${sender}: ${body}` : body
                };
            })
            .filter(Boolean);

        const latestMessage = visibleMessages.length
            ? visibleMessages[visibleMessages.length - 1].combined
            : "";

        const nearbyMessages = visibleMessages
            .slice(-5)
            .map((m) => m.combined)
            .join("\n");

        const recentRelationshipSummary = visibleMessages
            .slice(-3)
            .map((m) => m.combined)
            .join(" | ");

        return {
            latestMessage,
            nearbyMessages,
            recentRelationshipSummary
        };
    }

    function getXComposeContext(composer, container) {
        const scope =
            closestSafe(composer, '[role="dialog"]') ||
            closestSafe(composer, "main") ||
            container ||
            document;

        const form = closestSafe(composer, "form");
        const candidateTweets = Array.from(
            scope.querySelectorAll('article[data-testid="tweet"], [data-testid="tweet"]')
        ).filter((node) => !form?.contains(node));

        const primaryTweet = candidateTweets[0] || null;
        const sourceAuthor = getSourceAuthor(primaryTweet || scope);
        const sourceTitle = getSourceTitle(primaryTweet || scope);
        const sourceText = getSourceText(primaryTweet || scope);

        const nearbyTexts = [];
        const seen = new Set();
        for (const node of candidateTweets) {
            const text = getSourceText(node);
            if (!text || seen.has(text)) continue;
            seen.add(text);
            nearbyTexts.push(text);
            if (nearbyTexts.length >= 4) break;
        }

        return {
            sourceAuthor,
            sourceTitle: sourceTitle || document.title,
            sourceText,
            nearbyContextText: nearbyTexts.join("\n").slice(0, 1200)
        };
    }

    function buildContext(composer) {
        const { surface, container } = detectSurface(composer);
        const platform = getPlatformConfig();
        const platformKey = platform.key;
        const platformLabel = platform.label;
        const contactPrefix = platform.contactPrefix;

        if (surface === "start_post") {
            const xComposeContext =
                platformKey === "x" ? getXComposeContext(composer, container) : null;

            return {
                ContactId: `${contactPrefix}-post-compose`,
                RelationshipRole: "Peer",
                Platform: platformKey,
                Surface: "start_post",
                CurrentUrl: window.location.href,
                SourceAuthor: xComposeContext?.sourceAuthor || "",
                SourceTitle: xComposeContext?.sourceTitle || `${platformLabel} post composer`,
                SourceText: xComposeContext?.sourceText || "",
                ParentContextText: xComposeContext?.sourceText || "",
                NearbyContextText: xComposeContext?.nearbyContextText || "",
                LastInteractionDays: 0,
                TotalInteractions: 0,
                ReciprocityScore: 0,
                MomentumScore: 0,
                PowerDifferential: 0,
                EmotionalTemperature: 0,
                RecentRelationshipSummary: "",
                RelevantMemories: [],
                AllowNoReply: false,
                RequestAlternatives: false,
                InteractionMetadata: {
                    mode: "post",
                    pageTitle: document.title
                }
            };
        }

        if (surface === "messaging_chat") {
            const recipient = getMessageRecipientName(composer);
            const messageContext = getLatestMessageContext(composer);

            return {
                ContactId: recipient || `${contactPrefix}-message-contact`,
                RelationshipRole: "Peer",
                Platform: platformKey,
                Surface: "messaging_chat",
                CurrentUrl: window.location.href,
                SourceAuthor: recipient || "",
                SourceTitle: `${platformLabel} message thread`,
                SourceText: messageContext.latestMessage || "",
                ParentContextText: messageContext.nearbyMessages || "",
                NearbyContextText: messageContext.nearbyMessages || "",
                LastInteractionDays: 0,
                TotalInteractions: 0,
                ReciprocityScore: 0,
                MomentumScore: 0,
                PowerDifferential: 0,
                EmotionalTemperature: 0,
                RecentRelationshipSummary: messageContext.recentRelationshipSummary || "",
                RelevantMemories: [],
                AllowNoReply: true,
                RequestAlternatives: false,
                InteractionMetadata: {
                    mode: "message",
                    pageTitle: document.title,
                    recentMessageCount: messageContext.nearbyMessages
                        ? String(messageContext.nearbyMessages.split("\n").length)
                        : "0"
                }
            };
        }

        if (surface === "feed_reply") {
            const sourceAuthor = getSourceAuthor(container);
            const sourceTitle = getSourceTitle(container);
            const sourceText = getSourceText(container);
            const nearbyComments = getNearbyComments(container);

            return {
                ContactId: sourceAuthor || `${contactPrefix}-post-contact`,
                RelationshipRole: "Peer",
                Platform: platformKey,
                Surface: "feed_reply",
                CurrentUrl: window.location.href,
                SourceAuthor: sourceAuthor || "",
                SourceTitle: sourceTitle || "",
                SourceText: sourceText || "",
                ParentContextText: sourceText || "",
                NearbyContextText: nearbyComments || "",
                LastInteractionDays: 0,
                TotalInteractions: 0,
                ReciprocityScore: 0,
                MomentumScore: 0,
                PowerDifferential: 0,
                EmotionalTemperature: 0,
                RecentRelationshipSummary: "",
                RelevantMemories: [],
                AllowNoReply: true,
                RequestAlternatives: false,
                InteractionMetadata: {
                    mode: "reply",
                    pageTitle: document.title
                }
            };
        }

        return {
            ContactId: `${contactPrefix}-compose-contact`,
            RelationshipRole: "Peer",
            Platform: platformKey,
            Surface: "post_compose",
            CurrentUrl: window.location.href,
            SourceAuthor: "",
            SourceTitle: "",
            SourceText: "",
            ParentContextText: "",
            NearbyContextText: "",
            LastInteractionDays: 0,
            TotalInteractions: 0,
            ReciprocityScore: 0,
            MomentumScore: 0,
            PowerDifferential: 0,
            EmotionalTemperature: 0,
            RecentRelationshipSummary: "",
            RelevantMemories: [],
            AllowNoReply: true,
            RequestAlternatives: false,
            InteractionMetadata: {
                mode: "compose",
                pageTitle: document.title
            }
        };
    }

    function createPayload(composer) {
        const context = buildContext(composer);
        const platform = getPlatformConfig();

        return {
            UserId: "user-001",
            ContactId: context.ContactId || `${platform.contactPrefix}-compose-contact`,
            Message: getComposerText(composer),
            Platform: context.Platform || platform.key,
            Surface: context.Surface || "post_compose",
            CurrentUrl: context.CurrentUrl || "",
            SourceAuthor: context.SourceAuthor || "",
            SourceTitle: context.SourceTitle || "",
            SourceText: context.SourceText || "",
            ParentContextText: context.ParentContextText || "",
            NearbyContextText: context.NearbyContextText || "",
            RelationshipRole: context.RelationshipRole || "Peer",
            LastInteractionDays: context.LastInteractionDays ?? 0,
            TotalInteractions: context.TotalInteractions ?? 0,
            ReciprocityScore: context.ReciprocityScore ?? 0,
            MomentumScore: context.MomentumScore ?? 0,
            PowerDifferential: context.PowerDifferential ?? 0,
            EmotionalTemperature: context.EmotionalTemperature ?? 0,
            RecentRelationshipSummary: context.RecentRelationshipSummary || "",
            RelevantMemories: Array.isArray(context.RelevantMemories) ? context.RelevantMemories : [],
            AllowNoReply: typeof context.AllowNoReply === "boolean" ? context.AllowNoReply : true,
            RequestAlternatives: typeof context.RequestAlternatives === "boolean" ? context.RequestAlternatives : false,
            InteractionMetadata: context.InteractionMetadata || {}
        };
    }

    function extractSuggestion(data) {
        if (!data) return "";

        if (typeof data === "string") {
            return data.trim();
        }

        return (
            data.reply ||
            data.Reply ||
            data.summary ||
            data.rewrittenMessage ||
            data.message ||
            data.output ||
            data.suggestion ||
            data.text ||
            data.content ||
            data.result?.reply ||
            data.result?.message ||
            data.result?.output ||
            data.result?.suggestion ||
            data.decision?.reply ||
            data.decision?.message ||
            data.bestOption?.text ||
            data.best_option?.text ||
            data.candidate?.text ||
            data.candidates?.[0]?.text ||
            data.alternatives?.[0]?.text ||
            ""
        ).trim();
    }

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;");
    }

    function insertTextIntoEditor(composer, text) {
        if (!composer) return false;

        try {
            composer.focus();
            composer.innerHTML = `<p>${escapeHtml(text)}</p>`;

            composer.dispatchEvent(new InputEvent("input", {
                bubbles: true,
                inputType: "insertText",
                data: text
            }));
            composer.dispatchEvent(new Event("change", { bubbles: true }));
            composer.dispatchEvent(new KeyboardEvent("keydown", { bubbles: true, key: " " }));
            composer.dispatchEvent(new KeyboardEvent("keyup", { bubbles: true, key: " " }));

            return true;
        } catch (error) {
            console.error("[Sovereign] insert failed:", error);
            return false;
        }
    }

    async function getAuthToken() {
        const result = await chrome.storage.local.get([AUTH_KEY]);
        return result?.[AUTH_KEY] || "";
    }

    async function setPendingSuggestion(pending) {
        await chrome.storage.local.set({ [PENDING_KEY]: pending });
    }

    async function getPendingSuggestion() {
        const result = await chrome.storage.local.get([PENDING_KEY]);
        return result?.[PENDING_KEY] || null;
    }

    async function clearPendingSuggestion() {
        await chrome.storage.local.remove([PENDING_KEY]);
    }

    function openAuthPage() {
        chrome.runtime.sendMessage({ type: "SOVEREIGN_OPEN_AUTH" }, (res) => {
            if (chrome.runtime.lastError) {
                console.error("[Sovereign] auth open failed:", chrome.runtime.lastError.message);
                return;
            }

            console.log("[Sovereign] auth open response:", res);
        });
    }

    async function ensureAuthenticated(payload, slot) {
        const token = await getAuthToken();
        if (token) return token;

        await setPendingSuggestion({
            payload,
            currentUrl: window.location.href,
            createdAt: Date.now()
        });

        setStatus(slot, "Please log in to Sovereign first.", "info");
        openAuthPage();
        return "";
    }

    function callDecide(payload, button, slot, composer) {
        log("request payload", {
            surface: payload.Surface,
            contact: payload.ContactId,
            sourceAuthor: payload.SourceAuthor,
            sourceText: payload.SourceText,
            parentContext: payload.ParentContextText,
            nearbyContext: payload.NearbyContextText,
            payload
        });

        button.textContent = "Thinking...";
        button.setAttribute("data-sovereign-busy", "true");
        setStatus(slot, "Generating suggestion...", "info");

        chrome.runtime.sendMessage({ type: "SOVEREIGN_DECIDE", payload }, (res) => {
            button.textContent = "Suggest with Sovereign";
            button.setAttribute("data-sovereign-busy", "false");

            console.log("[Sovereign] full response from background:", res);
            console.log("[Sovereign] response data:", res?.data);

            if (chrome.runtime.lastError) {
                setStatus(slot, chrome.runtime.lastError.message || "Extension error.", "error");
                return;
            }

            if (!res?.ok) {
                setStatus(slot, res?.error || "Request failed.", "error");
                return;
            }

            const suggestion = extractSuggestion(res?.data);
            console.log("[Sovereign] extracted suggestion:", suggestion);

            if (!suggestion) {
                console.warn("[Sovereign] No suggestion extracted from:", res?.data);
                setStatus(slot, "No suggestion returned. Check console for response shape.", "error");
                return;
            }

            if (!insertTextIntoEditor(composer, suggestion)) {
                setStatus(slot, "Suggestion received, but insert failed.", "error");
                return;
            }

            setStatus(slot, "Suggestion inserted.", "success");
            setTimeout(() => clearStatus(slot), 2500);
        });
    }

    async function handleSuggestClick(composer, button, slot) {
        const message = getComposerText(composer);
        if (!message) {
            setStatus(slot, "Write something first.", "error");
            return;
        }

        const payload = createPayload(composer);
        const token = await ensureAuthenticated(payload, slot);
        if (!token) return;

        callDecide(payload, button, slot, composer);
    }

    async function tryResumePendingSuggestion(composer) {
        if (!composer) return;

        const token = await getAuthToken();
        if (!token) return;

        const pending = await getPendingSuggestion();
        if (!pending?.payload) return;

        const { surface } = detectSurface(composer);
        if (surface !== pending.payload.Surface) return;

        const host = getInjectionHost(composer);
        const slot = getOrCreateActionSlot(host, surface);
        if (!slot) return;

        let button = slot.querySelector(`.${BUTTON_CLASS}`);
        if (!button) {
            button = document.createElement("button");
            button.className = BUTTON_CLASS;
            button.type = "button";
            button.textContent = "Suggest with Sovereign";
            slot.prepend(button);
        }

        setStatus(slot, "Resuming your saved suggestion...", "info");
        await clearPendingSuggestion();
        callDecide(pending.payload, button, slot, composer);
    }

    function injectButton(composer) {
        if (!composer || !isVisible(composer)) return;

        const { surface } = detectSurface(composer);
        const host = getInjectionHost(composer);

        log("inject attempt", { surface, composer, host });

        if (!host || !isElement(host)) return;

        const slot = getOrCreateActionSlot(host, surface);
        if (!slot) return;

        let button = slot.querySelector(`.${BUTTON_CLASS}`);
        if (!button) {
            button = document.createElement("button");
            button.className = BUTTON_CLASS;
            button.type = "button";
            button.textContent = "Suggest with Sovereign";

            button.addEventListener("click", (event) => {
                event.preventDefault();
                event.stopPropagation();
                void handleSuggestClick(composer, button, slot);
            });

            slot.prepend(button);
            getOrCreateStatus(slot);
        }

        void tryResumePendingSuggestion(composer);
    }

    function findCandidateEditorsByQuery() {
        const all = Array.from(document.querySelectorAll('[contenteditable="true"], .ProseMirror'));
        const results = [];

        for (const el of all) {
            if (!isVisible(el)) continue;
            if (!isEditableTextbox(el)) continue;

            const { surface } = detectSurface(el);
            if (surface === "unknown") continue;

            results.push(el);
        }

        return dedupe(results);
    }

    function findComposerFromPath(path) {
        for (const node of path || []) {
            if (!isElement(node)) continue;

            if (isEditableTextbox(node)) {
                const { surface } = detectSurface(node);
                if (surface !== "unknown") return node;
            }

            const nested = node.querySelector?.('[contenteditable="true"], .ProseMirror');
            if (nested && isEditableTextbox(nested)) {
                const { surface } = detectSurface(nested);
                if (surface !== "unknown") return nested;
            }
        }

        return null;
    }

    function getDeepActiveElement(root = document) {
        let active = root.activeElement;

        while (active && active.shadowRoot && active.shadowRoot.activeElement) {
            active = active.shadowRoot.activeElement;
        }

        return active;
    }

    function tryInjectFromActiveElement() {
        const active = getDeepActiveElement(document);
        if (!active) return;

        let composer = null;

        if (isEditableTextbox(active)) {
            composer = active;
        } else {
            composer =
                closestSafe(active, '[contenteditable="true"]') ||
                closestSafe(active, ".ProseMirror");
        }

        if (composer && isEditableTextbox(composer)) {
            const { surface } = detectSurface(composer);
            if (surface !== "unknown") {
                log("activeElement composer", composer, surface);
                lastFocusedComposer = composer;
                injectButton(composer);
            }
        }
    }

    function scanForComposers() {
        ensureStyles();

        const composers = findCandidateEditorsByQuery();
        log("composers found", composers.length, composers);

        composers.forEach(injectButton);

        if (lastFocusedComposer) {
            injectButton(lastFocusedComposer);
        }
    }

    function startObserver() {
        if (observerStarted || !document.body) return;
        observerStarted = true;

        const observer = new MutationObserver(() => {
            ensureStyles();
            debounceScan();
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    function onFocusOrClick(event) {
        const path = event.composedPath ? event.composedPath() : [event.target];
        const composer = findComposerFromPath(path);

        log("event path composer", composer, event.type);

        if (composer) {
            lastFocusedComposer = composer;
            injectButton(composer);
        } else {
            tryInjectFromActiveElement();
        }
    }

    ensureStyles();
    startObserver();
    scanForComposers();

    document.addEventListener("focusin", onFocusOrClick, true);
    document.addEventListener("click", onFocusOrClick, true);
    window.addEventListener("load", debounceScan);
})();
