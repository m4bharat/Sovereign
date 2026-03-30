const DEFAULTS = {
    sovereignApiBaseUrl: "https://localhost:55270",
    sovereignToken: "",
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
        "sovereignToken",
        "sovereignUserId",
        "sovereignUseDecisionV2"
    ]);

    return {
        sovereignApiBaseUrl: settings.sovereignApiBaseUrl || DEFAULTS.sovereignApiBaseUrl,
        sovereignToken: settings.sovereignToken || DEFAULTS.sovereignToken,
        sovereignUserId: settings.sovereignUserId || DEFAULTS.sovereignUserId,
        sovereignUseDecisionV2: typeof settings.sovereignUseDecisionV2 === "boolean"
            ? settings.sovereignUseDecisionV2
            : DEFAULTS.sovereignUseDecisionV2
    };
}

function normalizeRequestBody(settings, payload) {
    const p = payload || {};

    return {
        UserId: p.UserId || p.userId || settings.sovereignUserId || "user-001",
        ContactId: p.ContactId || p.contactId || "linkedin-compose-contact",
        Message: p.Message || p.message || "",
        RelationshipRole: p.RelationshipRole || p.relationshipRole || "Peer",
        Platform: p.Platform || p.platform || "linkedin",
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

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (!message || message.type !== "SOVEREIGN_DECIDE") {
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
                    ...(settings.sovereignToken ? { Authorization: `Bearer ${settings.sovereignToken}` } : {})
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