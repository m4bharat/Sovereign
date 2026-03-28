function $(id) {
  return document.getElementById(id);
}

function setStatus(message) {
  $("status").textContent = message;
}

function loadSettings() {
  chrome.runtime.sendMessage({ type: "SOVEREIGN_GET_SETTINGS" }, response => {
    if (!response?.ok) {
      setStatus(response?.error || "Failed to load settings.");
      return;
    }

    const data = response.data || {};
    $("apiBaseUrl").value = data.sovereignApiBaseUrl || "";
    $("token").value = data.sovereignToken || "";
    $("userId").value = data.sovereignUserId || "";
    $("contactId").value = data.sovereignContactId || "";
    $("relationshipRole").value = data.sovereignRelationshipRole || "";
  });
}

function saveSettings() {
  const payload = {
    sovereignApiBaseUrl: $("apiBaseUrl").value.trim(),
    sovereignToken: $("token").value.trim(),
    sovereignUserId: $("userId").value.trim(),
    sovereignContactId: $("contactId").value.trim(),
    sovereignRelationshipRole: $("relationshipRole").value.trim()
  };

  chrome.runtime.sendMessage(
    {
      type: "SOVEREIGN_SAVE_SETTINGS",
      payload
    },
    response => {
      if (!response?.ok) {
        setStatus(response?.error || "Failed to save settings.");
        return;
      }

      setStatus("Settings saved.");
    }
  );
}

document.addEventListener("DOMContentLoaded", () => {
  loadSettings();
  $("saveBtn").addEventListener("click", saveSettings);
});
