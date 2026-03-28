const DEFAULTS = {
    sovereignApiBaseUrl: "https://localhost:55270",
    sovereignToken: "",
    sovereignUserId: "user-001",
    sovereignContactId: "linkedin-contact",
    sovereignRelationshipRole: "Peer"
};

chrome.runtime.onInstalled.addListener(() => {
    chrome.storage.local.get(null, existing => {
        chrome.storage.local.set({
            sovereignApiBaseUrl: existing.sovereignApiBaseUrl || DEFAULTS.sovereignApiBaseUrl,
            sovereignToken: existing.sovereignToken || DEFAULTS.sovereignToken,
            sovereignUserId: existing.sovereignUserId || DEFAULTS.sovereignUserId,
            sovereignContactId: existing.sovereignContactId || DEFAULTS.sovereignContactId,
            sovereignRelationshipRole: existing.sovereignRelationshipRole || DEFAULTS.sovereignRelationshipRole
        });
    });
});

async function getSettings() {
    return await chrome.storage.local.get([
        "sovereignApiBaseUrl",
        "sovereignToken",
        "sovereignUserId",
        "sovereignContactId",
        "sovereignRelationshipRole"
    ]);
}

async function callDecisionEndpoint(settings, payload) {
    const apiBaseUrl = settings.sovereignApiBaseUrl || DEFAULTS.sovereignApiBaseUrl;
    const token = settings.sovereignToken || "";

    const requestBody = {
        userId: settings.sovereignUserId || DEFAULTS.sovereignUserId,
        contactId: settings.sovereignContactId || DEFAULTS.sovereignContactId,
        relationshipRole: settings.sovereignRelationshipRole || DEFAULTS.sovereignRelationshipRole,
        message: payload?.message || ""
    };

    const response = await fetch(`${apiBaseUrl}/api/ai/conversations/decide`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            ...(token ? { Authorization: `Bearer ${token}` } : {})
        },
        body: JSON.stringify(requestBody)
    });

    const rawText = await response.text();

    let data = null;
    try {
        data = rawText ? JSON.parse(rawText) : null;
    } catch {
        data = { raw: rawText };
    }

    return {
        ok: response.ok,
        status: response.status,
        data,
        error: response.ok
            ? null
            : data?.message || data?.error || rawText || `HTTP ${response.status}`
    };
}

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
    if (message?.type === "SOVEREIGN_DECIDE") {
        getSettings()
            .then(settings => callDecisionEndpoint(settings, message.payload))
            .then(result => sendResponse(result))
            .catch(error => {
                sendResponse({
                    ok: false,
                    status: 0,
                    error: String(error)
                });
            });

        return true;
    }

    if (message?.type === "SOVEREIGN_GET_SETTINGS") {
        getSettings()
            .then(settings => sendResponse({ ok: true, data: settings }))
            .catch(error => sendResponse({ ok: false, error: String(error) }));

        return true;
    }

    if (message?.type === "SOVEREIGN_SAVE_SETTINGS") {
        chrome.storage.local.set({
            sovereignApiBaseUrl: message.payload?.sovereignApiBaseUrl || DEFAULTS.sovereignApiBaseUrl,
            sovereignToken: message.payload?.sovereignToken || "",
            sovereignUserId: message.payload?.sovereignUserId || DEFAULTS.sovereignUserId,
            sovereignContactId: message.payload?.sovereignContactId || DEFAULTS.sovereignContactId,
            sovereignRelationshipRole: message.payload?.sovereignRelationshipRole || DEFAULTS.sovereignRelationshipRole
        }, () => {
            if (chrome.runtime.lastError) {
                sendResponse({
                    ok: false,
                    error: chrome.runtime.lastError.message
                });
                return;
            }

            sendResponse({ ok: true });
        });

        return true;
    }

    return false;
});