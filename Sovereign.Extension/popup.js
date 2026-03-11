async function bootstrap() {
  const settings = await chrome.storage.local.get(['sovereignApiBaseUrl','sovereignToken','sovereignUserId','sovereignContactId','sovereignRelationshipRole']);
  document.getElementById('apiBaseUrl').value = settings.sovereignApiBaseUrl || 'https://localhost:5001';
  document.getElementById('token').value = settings.sovereignToken || '';
  document.getElementById('userId').value = settings.sovereignUserId || '';
  document.getElementById('contactId').value = settings.sovereignContactId || 'linkedin-contact';
  document.getElementById('relationshipRole').value = settings.sovereignRelationshipRole || 'Peer';
}
document.getElementById('save').onclick = async () => {
  await chrome.storage.local.set({
    sovereignApiBaseUrl: document.getElementById('apiBaseUrl').value.trim(),
    sovereignToken: document.getElementById('token').value.trim(),
    sovereignUserId: document.getElementById('userId').value.trim(),
    sovereignContactId: document.getElementById('contactId').value.trim(),
    sovereignRelationshipRole: document.getElementById('relationshipRole').value
  });
  document.getElementById('output').textContent = 'Saved.';
};
bootstrap();
