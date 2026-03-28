function findComposer() {
    const selectors = [
        'div[contenteditable="true"][role="textbox"]',
        'div.ql-editor[contenteditable="true"]',
        'div.comments-comment-box__editor'
    ];

    for (const selector of selectors) {
        const nodes = document.querySelectorAll(selector);
        for (const node of nodes) {
            if (node.offsetParent !== null) {
                return node;
            }
        }
    }

    return null;
}

function readComposerText() {
    const composer = findComposer();
    return composer?.innerText?.trim() || "";
}

function insertTextIntoComposer(text) {
    const target = findComposer();
    if (!target) return false;

    target.focus();

    if (document.execCommand) {
        target.innerHTML = "";
        document.execCommand("insertText", false, text);
        return true;
    }

    target.textContent = text;
    target.dispatchEvent(new Event("input", { bubbles: true }));
    return true;
}

function setButtonLoading(button, isLoading) {
    button.disabled = isLoading;
    button.textContent = isLoading ? "Sovereign is thinking..." : "Suggest with Sovereign";
    button.style.opacity = isLoading ? "0.7" : "1";
    button.style.cursor = isLoading ? "wait" : "pointer";
}

function createButton() {
    const button = document.createElement("button");
    button.id = "sovereign-linkedin-button";
    button.type = "button";
    button.textContent = "Suggest with Sovereign";
    button.style.cssText = [
        "margin:8px 0",
        "padding:10px 14px",
        "border-radius:999px",
        "border:none",
        "background:#0a66c2",
        "color:#fff",
        "font-size:14px",
        "font-weight:600",
        "cursor:pointer",
        "box-shadow:0 2px 8px rgba(0,0,0,0.12)",
        "display:block"
    ].join(";");

    button.addEventListener("click", () => {
        const message = readComposerText();

        if (!message) {
            alert("Type a draft first, then click Suggest with Sovereign.");
            return;
        }

        setButtonLoading(button, true);

        chrome.runtime.sendMessage(
            {
                type: "SOVEREIGN_DECIDE",
                payload: { message }
            },
            response => {
                setButtonLoading(button, false);

                if (chrome.runtime.lastError) {
                    alert(`Extension error: ${chrome.runtime.lastError.message}`);
                    return;
                }

                if (!response?.ok) {
                    console.error("Sovereign error response:", response);
                    alert(
                        response?.error ||
                        `Sovereign request failed (status ${response?.status || "unknown"}).`
                    );
                    return;
                }

                const suggestion = response?.data?.reply;

                if (!suggestion) {
                    console.error("Unexpected Sovereign success response:", response);
                    alert("Sovereign returned no reply.");
                    return;
                }

                const inserted = insertTextIntoComposer(suggestion);

                if (!inserted) {
                    alert("Sovereign generated a reply, but could not insert it into the composer.");
                }
            }
        );
    });

    return button;
}

function injectButton() {
    const composer = findComposer();
    if (!composer) return;

    const host = composer.parentElement || composer;
    if (!host) return;

    if (host.querySelector("#sovereign-linkedin-button")) return;

    const button = createButton();
    host.insertBefore(button, composer);
}

const observer = new MutationObserver(() => {
    injectButton();
});

observer.observe(document.body, {
    childList: true,
    subtree: true
});

injectButton();
setInterval(injectButton, 2000);