document.addEventListener("DOMContentLoaded", () => {
    const fields = ["apiBaseUrl", "token", "userId"];

    chrome.runtime.sendMessage({ type: "SOVEREIGN_GET_SETTINGS" }, (res) => {
        if (res?.data) {
            fields.forEach(f => document.getElementById(f).value = res.data[`sovereign${f.charAt(0).toUpperCase() + f.slice(1)}`] || "");
        }
    });

    document.getElementById("saveBtn").onclick = () => {
        const payload = {};
        fields.forEach(f => payload[`sovereign${f.charAt(0).toUpperCase() + f.slice(1)}`] = document.getElementById(f).value);
        chrome.runtime.sendMessage({ type: "SOVEREIGN_SAVE_SETTINGS", payload }, () => alert("Saved!"));
    };
});