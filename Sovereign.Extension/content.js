(() => {
    const BUTTON_ID = "sovereign-linkedin-button";
    const BUTTON_CONTAINER_ATTR = "data-sovereign-button-container";
    const POLL_INTERVAL_MS = 1800;

    function isVisible(node) {
        return !!node && node.offsetParent !== null;
    }

    function textOf(node) {
        return node?.innerText?.trim() || "";
    }

    function truncate(text, maxLength) {
        if (!text) return "";
        const clean = text.trim();
        return clean.length <= maxLength ? clean : `${clean.slice(0, maxLength)}...`;
    }

    function dedupeLines(text) {
        if (!text) return "";
        const seen = new Set();
        const lines = text
            .split("\n")
            .map(x => x.trim())
            .filter(Boolean)
            .filter(line => {
                if (seen.has(line)) return false;
                seen.add(line);
                return true;
            });

        return lines.join("\n");
    }

    function findComposer() {
        const selectors = [
            'form.comments-comment-box__form div[contenteditable="true"]',
            'div[contenteditable="true"][role="textbox"]',
            'div.ql-editor[contenteditable="true"]',
            'div.comments-comment-box__editor'
        ];

        for (const selector of selectors) {
            const nodes = document.querySelectorAll(selector);
            for (const node of nodes) {
                if (isVisible(node)) {
                    return node;
                }
            }
        }

        return null;
    }

    function readComposerText() {
        const composer = findComposer();
        return textOf(composer);
    }

    function insertTextIntoComposer(text) {
        const composer = findComposer();
        if (!composer) return false;

        composer.focus();

        try {
            const selection = window.getSelection();
            const range = document.createRange();
            range.selectNodeContents(composer);
            range.deleteContents();
            selection.removeAllRanges();
            selection.addRange(range);

            if (document.execCommand) {
                document.execCommand("insertText", false, text);
            } else {
                composer.textContent = text;
            }

            composer.dispatchEvent(new Event("input", { bubbles: true }));
            composer.dispatchEvent(new Event("change", { bubbles: true }));
            return true;
        } catch {
            composer.textContent = text;
            composer.dispatchEvent(new Event("input", { bubbles: true }));
            return true;
        }
    }

    function findClosestArticle(composer) {
        if (!composer) return null;

        return (
            composer.closest("article") ||
            composer.closest(".feed-shared-update-v2") ||
            composer.closest('[data-id]') ||
            composer.closest(".feed-shared-update-v2__content") ||
            null
        );
    }

    function findLinkedInMode() {
        const path = window.location.pathname.toLowerCase();

        if (path.includes("/messaging/")) return "chat";

        const composer = findComposer();
        const article = findClosestArticle(composer);

        if (article) return "reply";

        return "compose";
    }

    function getPageSurface(mode) {
        if (mode === "chat") return "messaging";
        if (mode === "reply") return "feed_reply";
        return "post_compose";
    }

    function getSourceText(article) {
        const selectors = [
            '[data-test-id="main-feed-activity-card__commentary"]',
            ".feed-shared-update-v2__description",
            ".update-components-text",
            ".feed-shared-text",
            ".comments-post-meta__description-text",
            ".feed-shared-inline-show-more-text",
            ".break-words"
        ];

        for (const selector of selectors) {
            const nodes = article
                ? article.querySelectorAll(selector)
                : document.querySelectorAll(selector);

            for (const node of nodes) {
                const text = textOf(node);
                if (text && text.length > 20) {
                    return truncate(dedupeLines(text), 3500);
                }
            }
        }

        if (article) {
            const fallback = truncate(dedupeLines(textOf(article)), 3500);
            return fallback;
        }

        return "";
    }

    function getSourceAuthor(article) {
        const selectors = [
            ".update-components-actor__title span[dir='ltr']",
            ".feed-shared-actor__name",
            ".comments-post-meta__name-text",
            "a[href*='/in/'] span[dir='ltr']",
            "a[href*='/company/'] span[dir='ltr']"
        ];

        for (const selector of selectors) {
            const node = article?.querySelector(selector) || document.querySelector(selector);
            const text = textOf(node);
            if (text) return truncate(text, 200);
        }

        return "";
    }

    function getSourceTitle(article) {
        const selectors = [
            ".update-components-actor__description",
            ".feed-shared-actor__description",
            ".comments-post-meta__headline",
            "h1"
        ];

        for (const selector of selectors) {
            const node = article?.querySelector(selector) || document.querySelector(selector);
            const text = textOf(node);
            if (text) return truncate(text, 500);
        }

        return "";
    }

    function getNearbyComments(article) {
        if (!article) return "";

        const selectors = [
            ".comments-comment-item-content-body",
            ".comments-comment-item",
            ".comments-comment-item__main-content"
        ];

        const collected = [];

        for (const selector of selectors) {
            const nodes = article.querySelectorAll(selector);

            for (const node of nodes) {
                const text = truncate(dedupeLines(textOf(node)), 350);
                if (text) {
                    collected.push(text);
                }
                if (collected.length >= 3) break;
            }

            if (collected.length >= 3) break;
        }

        return dedupeLines(collected.join("\n\n"));
    }

    function getChatContext() {
        const selectors = [
            ".msg-s-message-list__event",
            ".msg-s-event-listitem",
            ".msg-s-message-group"
        ];

        const collected = [];

        for (const selector of selectors) {
            const nodes = document.querySelectorAll(selector);

            for (const node of nodes) {
                const text = truncate(dedupeLines(textOf(node)), 500);
                if (text) {
                    collected.push(text);
                }
            }

            if (collected.length > 0) break;
        }

        return collected.slice(-6).join("\n\n");
    }

    function getCurrentChatParticipant() {
        const selectors = [
            ".msg-thread__link-to-profile",
            ".msg-entity-lockup__entity-title",
            ".artdeco-entity-lockup__title"
        ];

        for (const selector of selectors) {
            const node = document.querySelector(selector);
            const text = textOf(node);
            if (text) return truncate(text, 200);
        }

        return "";
    }

    function getCurrentUrl() {
        try {
            return window.location.href || "";
        } catch {
            return "";
        }
    }

    function buildInteractionMetadata(article, mode) {
        const metadata = {};

        metadata.pageTitle = document.title || "";
        metadata.mode = mode;

        const sourceProfileUrl =
            article?.querySelector("a[href*='/in/']")?.href ||
            article?.querySelector("a[href*='/company/']")?.href ||
            "";

        if (sourceProfileUrl) metadata.sourceProfileUrl = sourceProfileUrl;

        const articleId = article?.getAttribute("data-id") || "";
        if (articleId) metadata.articleId = articleId;

        return metadata;
    }

    function buildGenericContext() {
        const composer = findComposer();
        const article = findClosestArticle(composer);
        const mode = findLinkedInMode();

        if (mode === "chat") {
            const participant = getCurrentChatParticipant();

            return {
                platform: "linkedin",
                surface: getPageSurface(mode),
                currentUrl: getCurrentUrl(),
                sourceAuthor: participant,
                sourceTitle: "",
                sourceText: "",
                parentContextText: truncate(getChatContext(), 3500),
                nearbyContextText: "",
                interactionMetadata: buildInteractionMetadata(null, mode),
                contactId: participant || "linkedin-chat-contact",
                relationshipRole: "Peer"
            };
        }

        if (mode === "reply") {
            const sourceAuthor = getSourceAuthor(article);

            return {
                platform: "linkedin",
                surface: getPageSurface(mode),
                currentUrl: getCurrentUrl(),
                sourceAuthor,
                sourceTitle: getSourceTitle(article),
                sourceText: truncate(getSourceText(article), 3500),
                parentContextText: "",
                nearbyContextText: truncate(getNearbyComments(article), 1200),
                interactionMetadata: buildInteractionMetadata(article, mode),
                contactId: sourceAuthor || "linkedin-post-contact",
                relationshipRole: "Peer"
            };
        }

        return {
            platform: "linkedin",
            surface: getPageSurface(mode),
            currentUrl: getCurrentUrl(),
            sourceAuthor: "",
            sourceTitle: "",
            sourceText: "",
            parentContextText: "",
            nearbyContextText: "",
            interactionMetadata: buildInteractionMetadata(null, mode),
            contactId: "linkedin-compose-contact",
            relationshipRole: "Peer"
        };
    }

    function ensureButtonHost(composer) {
        if (!composer) return null;

        const form =
            composer.closest("form") ||
            composer.parentElement ||
            composer;

        return form;
    }

    function setButtonLoading(button, isLoading) {
        button.disabled = isLoading;
        button.textContent = isLoading
            ? "Sovereign is thinking..."
            : "Suggest with Sovereign";
        button.style.opacity = isLoading ? "0.7" : "1";
        button.style.cursor = isLoading ? "wait" : "pointer";
    }

    function createButton() {
        const button = document.createElement("button");
        button.id = BUTTON_ID;
        button.type = "button";
        button.textContent = "Suggest with Sovereign";
        button.style.cssText = [
            "margin:8px 0",
            "padding:10px 14px",
            "border-radius:999px",
            "border:none",
            "background:#0a66c2",
            "color:#fff",
            "font-size:14px",
            "font-weight:600",
            "cursor:pointer",
            "box-shadow:0 2px 8px rgba(0,0,0,0.12)",
            "display:block",
            "width:max-content"
        ].join(";");

        button.addEventListener("click", () => {
            const message = readComposerText();

            if (!message) {
                alert("Type a draft first, then click Suggest with Sovereign.");
                return;
            }

            const context = buildGenericContext();

            setButtonLoading(button, true);

            chrome.runtime.sendMessage(
                {
                    type: "SOVEREIGN_DECIDE",
                    payload: {
                        message,
                        ...context
                    }
                },
                response => {
                    setButtonLoading(button, false);

                    if (chrome.runtime.lastError) {
                        alert(`Extension error: ${chrome.runtime.lastError.message}`);
                        return;
                    }

                    if (!response?.ok) {
                        console.error("Sovereign error response:", response);
                        alert(
                            response?.error ||
                            `Sovereign request failed (status ${response?.status || "unknown"}).`
                        );
                        return;
                    }

                    const suggestion =
                        response?.data?.reply?.trim() ||
                        response?.data?.summary?.trim();

                    if (!suggestion) {
                        console.error("Unexpected Sovereign success response:", response);
                        alert("Sovereign returned no usable text.");
                        return;
                    }

                    const inserted = insertTextIntoComposer(suggestion);

                    if (!inserted) {
                        alert("Sovereign generated text, but could not insert it into the composer.");
                    }
                }
            );
        });

        return button;
    }

    function injectButton() {
        const composer = findComposer();
        if (!composer) return;

        const host = ensureButtonHost(composer);
        if (!host) return;

        if (host.querySelector(`#${BUTTON_ID}`)) return;

        const container = document.createElement("div");
        container.setAttribute(BUTTON_CONTAINER_ATTR, "true");
        container.style.cssText = "display:flex;align-items:center;gap:8px;flex-wrap:wrap;";

        const button = createButton();
        container.appendChild(button);

        if (host.firstChild) {
            host.insertBefore(container, host.firstChild);
        } else {
            host.appendChild(container);
        }
    }

    function bootstrap() {
        injectButton();

        const observer = new MutationObserver(() => {
            injectButton();
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });

        setInterval(injectButton, POLL_INTERVAL_MS);
    }

    bootstrap();
})();