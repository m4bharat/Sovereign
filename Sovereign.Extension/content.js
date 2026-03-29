(() => {
    const BUTTON_ID = "sovereign-linkedin-button";
    const POLL_INTERVAL_MS = 1500;

    function isVisible(node) { return !!node && node.offsetParent !== null; }

    // Updated parent finder based on your HTML: looking for role="listitem"
    function findFeedContainerFromComposer(composer) {
        if (!composer) return null;
        // In your HTML, the post is wrapped in a div with role="listitem"
        return composer.closest('[role="listitem"]') ||
            composer.closest(".feed-shared-update-v2") ||
            composer.closest("article");
    }

    function getSourceAuthor(container) {
        if (!container) return "";
        // Look for the profile link which usually contains the name in an aria-label
        const profileLink = container.querySelector('a[href*="/in/"]');
        if (profileLink) {
            // Check for the name inside the paragraph tag you provided
            const nameEl = profileLink.querySelector('p');
            if (nameEl) return nameEl.innerText.trim();
        }
        return "";
    }

    function getSourceText(container) {
        if (!container) return "";
        // In your HTML, the post text is inside a span with data-testid="expandable-text-box"
        const textEl = container.querySelector('[data-testid="expandable-text-box"]');
        return textEl ? textEl.innerText.trim().slice(0, 3500) : "";
    }

    function getSourceTitle(container) {
        if (!container) return "";
        // In your HTML, the "headline" (Senior Software Engineer...) is in a p tag
        // We look for a p tag that isn't the name or the suggested label
        const pTags = container.querySelectorAll('p');
        for (let p of pTags) {
            if (p.innerText.includes("|") || p.innerText.length > 30) {
                if (!p.getAttribute('data-testid')) return p.innerText.trim();
            }
        }
        return "";
    }

    function buildGenericContext(composer) {
        const container = findFeedContainerFromComposer(composer);

        if (container) {
            return {
                platform: "linkedin",
                surface: "feed_reply",
                currentUrl: window.location.href,
                sourceAuthor: getSourceAuthor(container),
                sourceText: getSourceText(container),
                sourceTitle: getSourceTitle(container),
                interactionMetadata: { mode: "reply", pageTitle: document.title }
            };
        }

        return {
            platform: "linkedin",
            surface: "post_compose",
            currentUrl: window.location.href,
            interactionMetadata: { mode: "compose" }
        };
    }

    function injectButton() {
        // Updated selector to match the ProseMirror editor in your HTML
        const composers = document.querySelectorAll(".ProseMirror, [contenteditable='true']");

        composers.forEach(composer => {
            if (!isVisible(composer) || composer.parentElement.querySelector(`#${BUTTON_ID}`)) return;

            const btn = document.createElement("button");
            btn.id = BUTTON_ID;
            btn.innerText = "Suggest with Sovereign";
            btn.style.cssText = "background:#0a66c2; color:white; border:none; border-radius:16px; padding:4px 12px; margin:4px; cursor:pointer; font-weight:600; font-size:12px; z-index:9999;";

            btn.onclick = (e) => {
                e.preventDefault();
                e.stopPropagation();

                const context = buildGenericContext(composer);
                const message = composer.innerText.trim();

                // Logging for you to debug in the Console (F12)
                console.log("Sovereign Context Captured:", context);

                btn.innerText = "Thinking...";
                chrome.runtime.sendMessage({ type: "SOVEREIGN_DECIDE", payload: { ...context, message } }, (res) => {
                    btn.innerText = "Suggest with Sovereign";
                    const reply = res?.data?.reply || res?.data?.summary;
                    if (reply) {
                        composer.focus();
                        document.execCommand("insertText", false, reply);
                    }
                });
            };

            // Insert after the composer
            composer.parentElement.appendChild(btn);
        });
    }

    setInterval(injectButton, POLL_INTERVAL_MS);
})();