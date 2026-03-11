chrome.runtime.onInstalled.addListener(() => {
  chrome.storage.local.set({
    sovereignApiBaseUrl: 'https://localhost:5001',
    sovereignToken: '',
    sovereignUserId: '',
    sovereignContactId: 'linkedin-contact',
    sovereignRelationshipRole: 'Peer'
  });
});

chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message?.type !== 'SOVEREIGN_REWRITE') {
    return false;
  }

  chrome.storage.local.get([
    'sovereignApiBaseUrl',
    'sovereignToken',
    'sovereignUserId',
    'sovereignContactId',
    'sovereignRelationshipRole'
  ]).then(async settings => {
    try {
      const response = await fetch(`${settings.sovereignApiBaseUrl || 'https://localhost:5001'}/api/ai/rewrite`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(settings.sovereignToken ? { Authorization: `Bearer ${settings.sovereignToken}` } : {})
        },
        body: JSON.stringify({
          userId: settings.sovereignUserId || 'user-001',
          contactId: settings.sovereignContactId || 'linkedin-contact',
          draft: message.payload?.draft || '',
          relationshipRole: settings.sovereignRelationshipRole || 'Peer',
          goal: 'Reconnect',
          platform: 'LinkedIn'
        })
      });

      const data = await response.json();
      sendResponse({ ok: response.ok, data });
    } catch (error) {
      sendResponse({ ok: false, error: String(error) });
    }
  });

  return true;
});
