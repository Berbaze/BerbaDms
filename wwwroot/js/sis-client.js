// ===================== SIS CLIENT =====================
let connection = new signalR.HubConnectionBuilder()
    .withUrl("/sisHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

let isConnected = false;
let pendingJoin = null;
let connectedUsers = []; // 🧠 Liste actuelle des utilisateurs connectés

// ===================== SIS LOG PANEL =====================
function createSISLogPanel() {
    if (document.getElementById("sisLogPanel")) return;

    const panel = document.createElement("div");
    panel.id = "sisLogPanel";
    panel.innerHTML = `
        <div id="sisLogHeader">🧠 SIS Echtzeit-Log</div>
        <div id="sisUserList">👥 Aktive Benutzer:<br><em>(keiner verbunden)</em></div>
        <div id="sisLogContainer"></div>
    `;
    document.body.appendChild(panel);

    const style = document.createElement("style");
    style.textContent = `
        #sisLogPanel {
            position: fixed;
            bottom: 20px;
            left: 20px;
            width: 400px;
            max-height: 300px;
            background: rgba(30, 30, 30, 0.95);
            color: #00ff88;
            font-family: monospace;
            font-size: 13px;
            border-radius: 10px;
            overflow-y: auto;
            box-shadow: 0 0 10px rgba(0,0,0,0.5);
            z-index: 9999;
            display: flex;
            flex-direction: column;
        }
        #sisLogHeader {
            background: #111;
            padding: 6px 10px;
            color: #0ff;
            font-weight: bold;
            border-bottom: 1px solid #333;
        }
        #sisUserList {
            padding: 6px 10px;
            border-bottom: 1px solid #333;
            color: #fff;
        }
        #sisLogContainer {
            flex: 1;
            padding: 8px;
            overflow-y: auto;
        }
        .sis-log-entry { margin-bottom: 4px; }
        .sis-log-join { color: #66ff66; }
        .sis-log-leave { color: #ff6666; }
        .sis-log-change { color: #00ffff; }
        .sis-log-info { color: #cccccc; }
    `;
    document.head.appendChild(style);

    // 🔄 Raccourci Alt + L pour cacher / afficher
    document.addEventListener("keydown", (e) => {
        if (e.key === "L" && e.altKey) {
            panel.style.display = (panel.style.display === "none") ? "flex" : "none";
        }
    });
}

// 🧠 Met à jour la liste des utilisateurs connectés
function updateUserListUI() {
    const listDiv = document.getElementById("sisUserList");
    if (!listDiv) return;

    if (connectedUsers.length === 0) {
        listDiv.innerHTML = "👥 Aktive Benutzer:<br><em>(keiner verbunden)</em>";
    } else {
        listDiv.innerHTML =
            "👥 Aktive Benutzer:<br>" + connectedUsers.map(u => "• " + u).join("<br>");
    }
}

// 🪶 Ajoute une entrée dans le log visuel
function logSIS(message, type = "info") {
    createSISLogPanel();
    const container = document.getElementById("sisLogContainer");
    if (!container) return;

    const entry = document.createElement("div");
    entry.classList.add("sis-log-entry", `sis-log-${type}`);
    const time = new Date().toLocaleTimeString();
    entry.textContent = `[${time}] ${message}`;
    container.appendChild(entry);
    container.scrollTop = container.scrollHeight;
}

// ===================== SignalR Events =====================

// 👋 Quand un utilisateur rejoint
connection.on("UserJoined", (userName) => {
    logSIS(`👋 ${userName} hat das Dokument betreten`, "join");
    showSISNotification(`${userName} hat die Sitzung betreten 👋`);
});

// 🚪 Quand un utilisateur quitte
connection.on("UserLeft", (userName) => {
    logSIS(`🚪 ${userName} hat das Dokument verlassen`, "leave");
    showSISNotification(`${userName} hat die Sitzung verlassen 👋`);
});

// 🔄 Liste des utilisateurs actifs (envoyée par le Hub)
connection.on("UpdateUserList", (users) => {
    connectedUsers = users;
    updateUserListUI();
    logSIS(`👥 Benutzerliste aktualisiert (${users.length})`, "info");
});

// 📩 Quand une modification est reçue
connection.on("ReceiveChange", (jsonData) => {
    const change = JSON.parse(jsonData);
    logSIS(`📩 Änderung erhalten von ${change.user} → ${change.type}`, "change");

    if (change.type === "signature") {
        applyRemoteSignature(change);
    } else if (change.type === "highlight") {
        applyRemoteHighlight(change);
    }
});

// ===================== Connexion =====================
async function startConnection() {
    try {
        await connection.start();
        isConnected = true;
        logSIS("✅ Verbunden mit SIS-Hub", "info");

        if (pendingJoin) {
            await joinDocumentSession(pendingJoin.docId, pendingJoin.user);
            pendingJoin = null;
        }
    } catch (err) {
        logSIS("❌ Verbindung fehlgeschlagen, neuer Versuch in 3s...", "leave");
        console.error("❌ Fehler beim Verbinden mit SIS:", err);
        setTimeout(startConnection, 3000);
    }
}

// ===================== Broadcast + Join =====================

// 📤 Envoie de changement à tous les autres
function broadcastChange(documentId, change) {
    if (!isConnected) {
        logSIS("⚠️ Noch nicht verbunden – Änderung wird ignoriert", "info");
        return;
    }

    connection.invoke("SendChange", documentId, JSON.stringify(change))
        .then(() => {
            logSIS(`📤 Änderung gesendet → ${change.type} von ${change.user}`, "change");
        })
        .catch(err => {
            logSIS("❌ Fehler beim Senden: " + err, "leave");
            console.error(err);
        });
}

// 📂 Rejoint une session collaborative
async function joinDocumentSession(documentId, userName) {
    if (!isConnected) {
        logSIS("⚠️ Verbindung noch nicht bereit, Join wird verzögert...", "info");
        pendingJoin = { docId: documentId, user: userName };
        return;
    }

    try {
        await connection.invoke("JoinDocument", documentId, userName);
        logSIS(`📂 Session beigetreten: ${documentId} als ${userName}`, "join");
    } catch (err) {
        logSIS("❌ Fehler beim Joinen: " + err, "leave");
        console.error(err);
    }
}

// ===================== Fonctions PDF Sync =====================

// 🖋️ Signatures distantes
function applyRemoteSignature(change) {
    logSIS(`🖋️ Remote-Signatur von ${change.user}`, "change");
    const container = document.querySelector(".pdf-page");
    if (!container) return;

    const box = document.createElement("div");
    box.classList.add("signature-box");
    box.style.left = change.x + "px";
    box.style.top = change.y + "px";
    box.style.width = change.w + "px";
    box.style.height = change.h + "px";
    box.style.position = "absolute";
    box.style.zIndex = "600";
    box.innerHTML = `<img src="${change.img}" 
        style="width:100%;height:100%;object-fit:contain;display:block;">`;

    const label = document.createElement("span");
    label.textContent = change.user;
    label.style.position = "absolute";
    label.style.bottom = "-18px";
    label.style.left = "0";
    label.style.fontSize = "12px";
    label.style.color = "blue";
    label.style.background = "rgba(255,255,255,0.6)";
    box.appendChild(label);

    container.appendChild(box);
}

// ✏️ Highlights distants
function applyRemoteHighlight(change) {
    if (!pdfDoc || !viewports[change.page]) return;
    const highlightCanvas = document.querySelector(`.pdf-page:nth-child(${change.page}) canvas:nth-child(2)`);
    if (!highlightCanvas) return;

    const ctx = highlightCanvas.getContext("2d");
    ctx.strokeStyle = change.color || "rgba(255,255,0,0.33)";
    ctx.lineWidth = 25;
    ctx.lineJoin = "round";
    ctx.lineCap = "round";
    ctx.beginPath();

    change.path.forEach((p, i) => {
        if (i === 0) ctx.moveTo(p.x, p.y);
        else ctx.lineTo(p.x, p.y);
    });
    ctx.stroke();
    logSIS(`🖍️ Remote-Highlight angewendet von ${change.user}`, "change");
}

// ===================== Notification visuelle =====================
function showSISNotification(message) {
    const div = document.createElement("div");
    div.textContent = message;
    div.style.position = "fixed";
    div.style.bottom = "20px";
    div.style.right = "20px";
    div.style.padding = "10px 16px";
    div.style.background = "rgba(0,0,0,0.8)";
    div.style.color = "#fff";
    div.style.borderRadius = "8px";
    div.style.fontSize = "14px";
    div.style.zIndex = "9999";
    div.style.transition = "opacity 0.3s";
    document.body.appendChild(div);

    setTimeout(() => {
        div.style.opacity = "0";
        setTimeout(() => div.remove(), 500);
    }, 2500);
}

// ===================== Start =====================
startConnection();
