(() => {
    const DEBUG = true;
    const BUTTON_CLASS = "sovereign-linkedin-button";
    const STATUS_CLASS = "sovereign-linkedin-status";
    const SLOT_CLASS = "sovereign-action-slot";
    const AUTH_KEY = "sovereignAuthToken";
    const PENDING_KEY = "sovereignPendingSuggestion";

    let scanTimer = null;
    let observerStarted = false;
    let lastFocusedComposer = null;

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
        if (document.getElementById("sovereign-inline-styles")) return;

        const style = document.createElement("style");
        style.id = "sovereign-inline-styles";
        style.textContent = `
            .${SLOT_CLASS} {
                display: flex;
                align-items: center;
                gap: 8px;
                margin: 8px 0;
                padding: 4px 0;
                flex-wrap: wrap;
                position: relative;
                z-index: 999999;
            }

            .${BUTTON_CLASS} {
                background: #0a66c2;
                color: #fff;
                border: none;
                border-radius: 999px;
                padding: 6px 12px;
                cursor: pointer;
                font-weight: 600;
                font-size: 12px;
                line-height: 1.2;
                white-space: nowrap;
                margin: 0;
                z-index: 999999;
            }

            .${BUTTON_CLASS}[data-sovereign-busy="true"] {
                opacity: 0.72;
                cursor: wait;
            }

            .${STATUS_CLASS} {
                font-size: 12px;
                line-height: 1.3;
                color: #666;
                max-width: 420px;
                word-break: break-word;
            }

            .${STATUS_CLASS}[data-type="error"] {
                color: #b00020;
            }

            .${STATUS_CLASS}[data-type="success"] {
                color: #1a7f37;
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

        if (!isContentEditable && !isProseMirror) return false;

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
            ariaLabel.includes("write") ||
            ariaLabel.includes("message") ||
            ariaLabel.includes("text editor") ||
            placeholder.includes("write") ||
            placeholder.includes("message") ||
            placeholder.includes("talk about")
        );
    }

    function detectSurface(composer) {
        if (!composer) return { surface: "unknown", container: null };

        const shareBox = closestSafe(composer, ".share-box");
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

    function getInjectionHost(composer) {
        const { surface, container } = detectSurface(composer);

        if (surface === "start_post") {
            return (
                container?.querySelector(".share-creation-state__footer") ||
                container?.querySelector(".share-creation-state__additional-toolbar") ||
                container?.querySelector(".share-creation-state") ||
                container
            );
        }

        if (surface === "messaging_chat") {
            const msgForm = closestSafe(composer, ".msg-form") || container;
            return (
                msgForm?.querySelector(".msg-form__footer") ||
                msgForm?.querySelector(".msg-form__msg-content-container") ||
                msgForm
            );
        }

        if (surface === "feed_reply") {
            return composer.parentElement || container;
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
        host.prepend(slot);
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

    function buildContext(composer) {
        const { surface, container } = detectSurface(composer);

        if (surface === "start_post") {
            return {
                ContactId: "linkedin-post-compose",
                RelationshipRole: "Peer",
                Platform: "linkedin",
                Surface: "start_post",
                CurrentUrl: window.location.href,
                SourceAuthor: "",
                SourceTitle: "LinkedIn post composer",
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
                ContactId: recipient || "linkedin-message-contact",
                RelationshipRole: "Peer",
                Platform: "linkedin",
                Surface: "messaging_chat",
                CurrentUrl: window.location.href,
                SourceAuthor: recipient || "",
                SourceTitle: "LinkedIn message thread",
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
                ContactId: sourceAuthor || "linkedin-post-contact",
                RelationshipRole: "Peer",
                Platform: "linkedin",
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
            ContactId: "linkedin-compose-contact",
            RelationshipRole: "Peer",
            Platform: "linkedin",
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

        return {
            UserId: "user-001",
            ContactId: context.ContactId || "linkedin-compose-contact",
            Message: getComposerText(composer),
            Platform: context.Platform || "linkedin",
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
        if (pending.currentUrl && pending.currentUrl !== window.location.href) return;

        const { surface } = detectSurface(composer);
        if (surface !== pending.payload.Surface) return;

        const host = getInjectionHost(composer);
        const slot = getOrCreateActionSlot(host, surface);
        if (!slot) return;

        const button = slot.querySelector(`.${BUTTON_CLASS}`) || document.createElement("button");
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
