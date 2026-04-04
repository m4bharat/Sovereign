(() => {
    const BUTTON_ID = "sovereign-linkedin-button";
    const INJECTED_COMPOSERS = new Set();

    function isVisible(node) {
        return !!node && node.offsetParent !== null;
    }

    function findFeedContainerFromComposer(composer) {
        if (!composer) return null;

        return composer.closest('[role="listitem"]') ||
            composer.closest(".feed-shared-update-v2") ||
            composer.closest("article");
    }

    function getSourceAuthor(container) {
        if (!container) return "";

        const candidates = container.querySelectorAll('a[href*="/in/"], a[href*="/company/"]');

        for (const link of candidates) {
            const p = link.querySelector("p");
            if (p) {
                const text = p.innerText.trim();
                if (
                    text &&
                    !text.toLowerCase().includes("follow") &&
                    !text.toLowerCase().includes("suggested")
                ) {
                    return text;
                }
            }

            const span = link.querySelector("span");
            if (span) {
                const text = span.innerText.trim();
                if (
                    text &&
                    !text.toLowerCase().includes("follow") &&
                    !text.toLowerCase().includes("suggested")
                ) {
                    return text;
                }
            }
        }

        return "";
    }

    function getSourceText(container) {
        if (!container) return "";

        const textEl = container.querySelector('[data-testid="expandable-text-box"]');
        return textEl ? textEl.innerText.trim().slice(0, 3500) : "";
    }

    function getSourceTitle(container) {
        if (!container) return "";

        const pTags = container.querySelectorAll("p");
        for (const p of pTags) {
            const text = p.innerText.trim();
            if (!text) continue;

            if (
                !text.toLowerCase().includes("suggested") &&
                !text.toLowerCase().includes("follow") &&
                text !== getSourceAuthor(container) &&
                (text.includes("|") || text.length > 30)
            ) {
                return text;
            }
        }

        return "";
    }

    function buildGenericContext(composer) {
        const container = findFeedContainerFromComposer(composer);

        if (container) {
            const sourceAuthor = getSourceAuthor(container);
            const sourceText = getSourceText(container);
            const sourceTitle = getSourceTitle(container);

            return {
                ContactId: sourceAuthor || "linkedin-post-contact",
                RelationshipRole: "Peer",
                Platform: "linkedin",
                Surface: "feed_reply",
                CurrentUrl: window.location.href,
                SourceAuthor: sourceAuthor,
                SourceText: sourceText,
                SourceTitle: sourceTitle,
                ParentContextText: sourceText,
                NearbyContextText: "",
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
            SourceText: "",
            SourceTitle: "",
            ParentContextText: "",
            NearbyContextText: "",
            InteractionMetadata: {
                mode: "compose",
                pageTitle: document.title
            }
        };
    }

    function extractSuggestion(data) {
        if (!data) return "";

        return (
            data.reply ||
            data.Reply ||
            data.summary ||
            data.rewrittenMessage ||
            data.message ||
            data.output ||
            ""
        ).trim();
    }

    function injectButton(composer) {
        if (!isVisible(composer)) return;
        if (!composer.parentElement) return;
        if (composer.parentElement.querySelector(`#${BUTTON_ID}`)) return;

        const btn = document.createElement("button");
        btn.id = BUTTON_ID;
        btn.innerText = "Suggest with Sovereign";
        btn.style.cssText = "background:#0a66c2; color:white; border:none; border-radius:16px; padding:4px 12px; margin:4px; cursor:pointer; font-weight:600; font-size:12px; z-index:9999;";

        btn.onclick = (e) => {
            e.preventDefault();
            e.stopPropagation();

            const context = buildGenericContext(composer);
            const message = composer.innerText.trim();

            const payload = {
                UserId: "user-001",
                ContactId: context.ContactId || "linkedin-compose-contact",
                Message: message,
                RelationshipRole: context.RelationshipRole || "Peer",
                Platform: context.Platform || "linkedin",
                Surface: context.Surface || "post_compose",
                CurrentUrl: context.CurrentUrl || "",
                SourceAuthor: context.SourceAuthor || "",
                SourceTitle: context.SourceTitle || "",
                SourceText: context.SourceText || "",
                ParentContextText: context.ParentContextText || "",
                NearbyContextText: context.NearbyContextText || "",
                InteractionMetadata: context.InteractionMetadata || {}
            };

            console.log("Sovereign Context Captured:", context);
            console.log("Sovereign Final Payload:", payload);

            btn.innerText = "Thinking...";

            chrome.runtime.sendMessage(
                {
                    type: "SOVEREIGN_DECIDE",
                    payload
                },
                (res) => {
                    btn.innerText = "Suggest with Sovereign";

                    if (chrome.runtime.lastError) {
                        console.error("Sovereign extension error:", chrome.runtime.lastError);
                        return;
                    }

                    console.log("Sovereign API Response:", res);

                    const reply = extractSuggestion(res?.data);

                    if (reply) {
                        composer.focus();
                        document.execCommand("insertText", false, reply);
                    }
                }
            );
        };

        composer.parentElement.appendChild(btn);
    }

    function scanForComposers() {
        const composers = document.querySelectorAll(".ProseMirror, [contenteditable='true']:not(#suggestionBox)");

        composers.forEach((composer) => {
            if (!isVisible(composer)) return;
            if (!composer.parentElement) return;
            if (composer.parentElement.querySelector(`#${BUTTON_ID}`)) return;
            if (INJECTED_COMPOSERS.has(composer)) return;

            INJECTED_COMPOSERS.add(composer);
            injectButton(composer);
        });
    }

    // Use MutationObserver for reliable surface detection instead of interval polling
    const observer = new MutationObserver(() => {
        scanForComposers();
    });

    // Start observing the document for changes
    observer.observe(document.body, {
        childList: true,
        subtree: true,
        attributes: false,
        characterData: false
    });

    // Initial scan on load
    scanForComposers();
})();