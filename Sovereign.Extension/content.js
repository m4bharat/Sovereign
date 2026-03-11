function findPrimaryLinkedInText() {
  const selectors = ['.feed-shared-update-v2__description','.comments-post-meta__description-text','.update-components-text','.feed-shared-text'];
  for (const selector of selectors) {
    const node = document.querySelector(selector);
    const text = node?.innerText?.trim();
    if (text) return text;
  }
  return document.body.innerText.slice(0, 1200);
}

async function injectSovereignButton() {
  if (document.getElementById('sovereign-linkedin-button')) return;
  const composer = document.querySelector('div.comments-comment-box__form, form.comments-comment-box__form, div.ql-editor[contenteditable="true"]');
  if (!composer) return;

  const button = document.createElement('button');
  button.id = 'sovereign-linkedin-button';
  button.type = 'button';
  button.textContent = 'Suggest with Sovereign';
  button.style.cssText = 'margin:8px 0;padding:10px 14px;border-radius:999px;border:none;background:#0a66c2;color:white;font-weight:600;cursor:pointer;';

  button.addEventListener('click', async () => {
    chrome.runtime.sendMessage(
      {
        type: 'SOVEREIGN_REWRITE',
        payload: { draft: findPrimaryLinkedInText() }
      },
      response => {
        if (!response?.ok) {
          alert('Sovereign request failed. Check API base URL and token.');
          return;
        }

        const bestVariant = response?.data?.variants?.[0]?.message;
        const target = document.querySelector('div.ql-editor[contenteditable="true"]');

        if (bestVariant && target) {
          target.innerHTML = '';
          target.focus();
          document.execCommand('insertText', false, bestVariant);
        } else {
          alert('Sovereign could not generate a comment.');
        }
      }
    );
  });

  composer.parentElement?.insertBefore(button, composer);
}
setInterval(injectSovereignButton, 2000);
