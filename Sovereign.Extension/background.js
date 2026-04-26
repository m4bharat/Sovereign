const DEFAULTS = {
    sovereignApiBaseUrl: "https://localhost:55270",
    sovereignAuthToken: "",
    sovereignUserId: "user-001",
    sovereignUseDecisionV2: true
};

function getStorage(keys) {
    return new Promise((resolve) => {
        chrome.storage.local.get(keys, (result) => resolve(result || {}));
    });
}

async function getSettings() {
    const settings = await getStorage([
        "sovereignApiBaseUrl",
        "sovereignAuthToken",
        "sovereignToken",
        "sovereignUserId",
        "sovereignUseDecisionV2",
        "sovereignAuthUrl"
    ]);

    const authToken =
        settings.sovereignAuthToken ||
        settings.sovereignToken ||
        DEFAULTS.sovereignAuthToken;

    return {
        sovereignApiBaseUrl: settings.sovereignApiBaseUrl || DEFAULTS.sovereignApiBaseUrl,
        sovereignAuthToken: authToken,
        sovereignUserId: settings.sovereignUserId || DEFAULTS.sovereignUserId,
        sovereignUseDecisionV2:
            typeof settings.sovereignUseDecisionV2 === "boolean"
                ? settings.sovereignUseDecisionV2
                : DEFAULTS.sovereignUseDecisionV2,
        sovereignAuthUrl:
            settings.sovereignAuthUrl || chrome.runtime.getURL("ui/browser/index.html#/auth")
    };
}

function normalizeRequestBody(settings, payload) {
    const p = payload || {};

    return {
        UserId: p.UserId || p.userId || settings.sovereignUserId || "user-001",
        ContactId: p.ContactId || p.contactId || "social-compose-contact",
        Message: p.Message || p.message || "",
        RelationshipRole: p.RelationshipRole || p.relationshipRole || "Peer",
        Platform: p.Platform || p.platform || "social",
        Surface: p.Surface || p.surface || "post_compose",
        CurrentUrl: p.CurrentUrl || p.currentUrl || "",
        SourceAuthor: p.SourceAuthor || p.sourceAuthor || "",
        SourceTitle: p.SourceTitle || p.sourceTitle || "",
        SourceText: p.SourceText || p.sourceText || "",
        ParentContextText: p.ParentContextText || p.parentContextText || "",
        NearbyContextText: p.NearbyContextText || p.nearbyContextText || "",
        InteractionMetadata: p.InteractionMetadata || p.interactionMetadata || {}
    };
}

function extractErrorMessage(status, data, rawText) {
    if (data && data.error) return data.error;
    if (data && data.title) return data.title;
    if (rawText && rawText.trim()) return rawText.trim();
    return `HTTP ${status}`;
}

async function focusBestSocialTab() {
    const tabGroups = await Promise.all([
        chrome.tabs.query({ url: "https://www.linkedin.com/*" }),
        chrome.tabs.query({ url: "https://x.com/*" }),
        chrome.tabs.query({ url: "https://www.x.com/*" }),
        chrome.tabs.query({ url: "https://x.xom/*" }),
        chrome.tabs.query({ url: "https://www.x.xom/*" })
    ]);

    const tabs = tabGroups.flat();
    if (!tabs.length) return;

    const activeTab = tabs.find((t) => t.active) || tabs[0];
    if (activeTab.windowId != null) {
        await chrome.windows.update(activeTab.windowId, { focused: true });
    }
    if (activeTab.id != null) {
        await chrome.tabs.update(activeTab.id, { active: true });
    }
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (!message) {
        return false;
    }

    if (message.type === "SOVEREIGN_GET_SETTINGS") {
        getSettings()
            .then((settings) => {
                sendResponse({ ok: true, data: settings });
            })
            .catch((error) => {
                sendResponse({ ok: false, error: error?.message || String(error) });
            });
        return true;
    }

    if (message.type === "SOVEREIGN_SAVE_SETTINGS") {
        const payload = message.payload || {};
        chrome.storage.local.set(payload, () => {
            sendResponse({ ok: true, message: "Settings saved" });
        });
        return true;
    }

    if (message.type === "SOVEREIGN_OPEN_AUTH") {
        getSettings()
            .then((settings) => {
                chrome.tabs.create({ url: settings.sovereignAuthUrl }, () => {
                    sendResponse({ ok: true });
                });
            })
            .catch((error) => {
                sendResponse({ ok: false, error: error?.message || String(error) });
            });
        return true;
    }

    if (message.type === "SOVEREIGN_AUTH_COMPLETED") {
        focusBestSocialTab()
            .then(() => sendResponse({ ok: true }))
            .catch((error) => sendResponse({ ok: false, error: error?.message || String(error) }));
        return true;
    }

    if (message.type !== "SOVEREIGN_DECIDE") {
        return false;
    }

    getSettings()
        .then(async (settings) => {
            const endpoint = settings.sovereignUseDecisionV2
                ? "/api/ai/conversations/decide-v2"
                : "/api/ai/conversations/decide";

            const requestBody = normalizeRequestBody(settings, message.payload);

            console.log("[Sovereign background] normalized request body:", requestBody);

            const response = await fetch(`${settings.sovereignApiBaseUrl}${endpoint}`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    ...(settings.sovereignAuthToken
                        ? { Authorization: `Bearer ${settings.sovereignAuthToken}` }
                        : {})
                },
                body: JSON.stringify(requestBody)
            });

            const rawText = await response.text();
            let data = null;

            try {
                data = rawText ? JSON.parse(rawText) : {};
            } catch {
                data = null;
            }

            if (!response.ok) {
                sendResponse({
                    ok: false,
                    status: response.status,
                    error: extractErrorMessage(response.status, data, rawText),
                    data: data || rawText
                });
                return;
            }

            sendResponse({
                ok: true,
                status: response.status,
                data: data || {}
            });
        })
        .catch((error) => {
            sendResponse({
                ok: false,
                error: error && error.message ? error.message : String(error)
            });
        });

    return true;
});
