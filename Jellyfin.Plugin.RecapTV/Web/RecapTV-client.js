// Finds the preferences-menu row to clone by matching visible label text (e.g. "Controls") instead of hashed MUI classnames; breaks if the client localizes, virtualizes the list, or goes icon-only.
(function () {
  var ENTRY_ID = "RecapTV-menu-entry";
  var ANCHOR_LABELS = ["Controls", "Subtitles", "Playback", "Home", "Display", "Quick Connect", "Profile"];
  var STYLE_ID = "RecapTV-style";

  function ensureStyles() {
    if (document.getElementById(STYLE_ID)) return;
    var style = document.createElement("style");
    style.id = STYLE_ID;
    style.textContent =
      ".RecapTV-modal{position:fixed;inset:0;z-index:10000;background:rgba(0,0,0,.6);" +
      "display:flex;align-items:center;justify-content:center;font-family:sans-serif}" +
      ".RecapTV-card{background:#202020;color:#fff;border-radius:8px;padding:1.5rem;width:22rem;max-width:90vw}" +
      ".RecapTV-card h2{margin:0 0 .75rem;font-size:1.1rem}" +
      ".RecapTV-card p{font-size:.85rem;opacity:.8;margin:.25rem 0 1rem}" +
      ".RecapTV-card input{width:100%;box-sizing:border-box;padding:.5rem;margin-bottom:.75rem;" +
      "border-radius:4px;border:1px solid #444;background:#111;color:#fff}" +
      ".RecapTV-card button{padding:.5rem 1rem;border-radius:4px;border:none;cursor:pointer;margin-right:.5rem}" +
      ".RecapTV-save{background:#00a4dc;color:#fff}.RecapTV-disconnect{background:#a33;color:#fff}" +
      ".RecapTV-close{background:transparent;color:#ccc}.RecapTV-err{color:#f66;font-size:.8rem;min-height:1.2em}" +
      ".RecapTV-err.RecapTV-success{color:#4caf50}";
    document.head.appendChild(style);
  }

  function api(method, path, body) {
    return window.ApiClient.ajax({
      type: method,
      url: window.ApiClient.getUrl("RecapTV/" + path),
      data: body ? JSON.stringify(body) : undefined,
      contentType: body ? "application/json" : undefined,
      dataType: "json"
    });
  }

  function openModal() {
    closeModal();
    ensureStyles();

    var overlay = document.createElement("div");
    overlay.className = "RecapTV-modal";
    overlay.id = "RecapTV-modal";
    overlay.innerHTML =
      '<div class="RecapTV-card">' +
      "<h2>Connect RecapTV</h2>" +
      '<p id="RecapTV-status">Checking connection…</p>' +
      '<input id="RecapTV-token" type="password" placeholder="Paste your RecapTV token" autocomplete="new-password" />' +
      '<div class="RecapTV-err" id="RecapTV-err"></div>' +
      '<button class="RecapTV-save" id="RecapTV-save">Save</button>' +
      '<button class="RecapTV-disconnect" id="RecapTV-disconnect" style="display:none">Disconnect</button>' +
      '<button class="RecapTV-close" id="RecapTV-close">Close</button>' +
      "</div>";
    document.body.appendChild(overlay);

    overlay.addEventListener("click", function (e) {
      if (e.target === overlay) closeModal();
    });
    document.getElementById("RecapTV-close").onclick = closeModal;
    document.getElementById("RecapTV-save").onclick = save;
    document.getElementById("RecapTV-disconnect").onclick = disconnect;

    refreshStatus();
  }

  function closeModal() {
    var existing = document.getElementById("RecapTV-modal");
    if (existing) existing.remove();
  }

  function applyStatus(res) {
    var status = document.getElementById("RecapTV-status");
    var disconnectBtn = document.getElementById("RecapTV-disconnect");
    var tokenInput = document.getElementById("RecapTV-token");
    if (!status) return;
    if (res.connected) {
      status.textContent = res.lastError
        ? "Connected, but RecapTV rejected the last sync: " + res.lastError
        : "Connected.";
      disconnectBtn.style.display = "inline-block";
      if (tokenInput) tokenInput.placeholder = "Token configured - paste to replace";
    } else {
      status.textContent = "Not connected. Paste a token from RecapTV → Integrations → Jellyfin.";
      disconnectBtn.style.display = "none";
      if (tokenInput) tokenInput.placeholder = "Paste your RecapTV token";
    }
  }

  function refreshStatus() {
    // Timestamp forces a distinct URL each call - ApiClient.ajax appears to dedupe identical GET URLs.
    api("GET", "Status?_=" + Date.now()).then(applyStatus);
  }

  function save() {
    var token = document.getElementById("RecapTV-token").value.trim();
    var err = document.getElementById("RecapTV-err");
    err.textContent = "";
    err.classList.remove("RecapTV-success");
    if (!token) {
      err.textContent = "Enter a token first.";
      return;
    }

    api("POST", "Token", { token: token }).then(
      function () {
        document.getElementById("RecapTV-token").value = "";
        err.textContent = "Token saved.";
        err.classList.add("RecapTV-success");
        refreshStatus();
      },
      function () {
        err.textContent = "Could not save token. Try again.";
        err.classList.remove("RecapTV-success");
      }
    );
  }

  function disconnect() {
    api("DELETE", "Token").then(refreshStatus);
  }

  function findLabelNode(root) {
    var walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
    var node;
    while ((node = walker.nextNode())) {
      if (node.textContent.trim()) return node;
    }
    return null;
  }

  function findAnchorRow() {
    var candidates = document.querySelectorAll("a, li, [role='button']");
    for (var l = 0; l < ANCHOR_LABELS.length; l++) {
      for (var i = 0; i < candidates.length; i++) {
        if (candidates[i].textContent.trim() === ANCHOR_LABELS[l]) {
          return candidates[i];
        }
      }
    }
    return null;
  }

  function ensureMenuEntry() {
    var anchorRow = findAnchorRow();
    var list = anchorRow && anchorRow.parentElement;
    if (!list) return;

    var existing = document.getElementById(ENTRY_ID);
    if (existing) {
      // Only trusts an existing entry if it's in the currently-visible list - some SPA route transitions leave a stale, hidden clone attached alongside a freshly mounted list.
      if (existing.parentElement === list) return;
      existing.remove();
    }

    var entry = anchorRow.cloneNode(true);
    entry.id = ENTRY_ID;
    entry.removeAttribute("href");

    var label = findLabelNode(entry);
    if (label) label.textContent = "RecapTV";

    var fontIcon = entry.querySelector(".material-icons");
    if (fontIcon) {
      fontIcon.classList.remove.apply(
        fontIcon.classList,
        Array.prototype.filter.call(fontIcon.classList, function (c) {
          return c !== "material-icons" && c.indexOf("listItemIcon") !== 0;
        })
      );
      fontIcon.classList.add("live_tv");
    }

    entry.addEventListener("click", function (e) {
      e.preventDefault();
      e.stopPropagation();
      openModal();
    });

    list.appendChild(entry);
  }

  // MutationObserver reacts immediately when the React-driven menu re-renders and wipes our clone; the 1s interval is a backstop for removals that don't fire through observed mutations.
  var observer = new MutationObserver(ensureMenuEntry);
  observer.observe(document.body, { childList: true, subtree: true });

  setInterval(ensureMenuEntry, 1000);
  ensureMenuEntry();
})();
