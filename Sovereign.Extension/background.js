const DEFAULTS = {
  sovereignApiBaseUrl: 'https://localhost:55270',
  sovereignToken: '',
  sovereignUserId: 'user-001',
  sovereignContactId: 'generic-contact',
  sovereignRelationshipRole: 'Peer'
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
    'sovereignApiBaseUrl',
    'sovereignToken',
    'sovereignUserId',
    'sovereignContactId',
    'sovereignRelationshipRole'
  ]);
}

async function callDecisionEndpoint(settings, payload) {
  const apiBaseUrl = (settings.sovereignApiBaseUrl || DEFAULTS.sovereignApiBaseUrl).trim();
  const token = (settings.sovereignToken || '').trim();

  const requestBody = {
    userId: (settings.sovereignUserId || DEFAULTS.sovereignUserId).trim(),
    contactId: (payload?.contactId || settings.sovereignContactId || DEFAULTS.sovereignContactId).trim(),
    relationshipRole: (payload?.relationshipRole || settings.sovereignRelationshipRole || DEFAULTS.sovereignRelationshipRole).trim(),
    message: payload?.message || '',
    platform: payload?.platform || '',
    surface: payload?.surface || '',
    currentUrl: payload?.currentUrl || '',
    sourceAuthor: payload?.sourceAuthor || '',
    sourceTitle: payload?.sourceTitle || '',
    sourceText: payload?.sourceText || '',
    parentContextText: payload?.parentContextText || '',
    nearbyContextText: payload?.nearbyContextText || '',
    interactionMetadata: payload?.interactionMetadata || {}
  };

  const response = await fetch(`${apiBaseUrl}/api/ai/conversations/decide`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
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
  if (message?.type !== 'SOVEREIGN_DECIDE') {
    return false;
  }

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
});
