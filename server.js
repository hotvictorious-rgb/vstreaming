const fs = require('fs');
const path = require('path');
const http = require('http');
const https = require('https');
const express = require('express');
const { WebSocketServer, WebSocket } = require('ws');
const selfsigned = require('selfsigned');
const qrcode = require('qrcode');
const os = require('os');
const multer = require('multer');

// Initialize Express App
const app = express();
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));

// Detect Local LAN IP (Wi-Fi or Ethernet)
function getLocalIP() {
    const interfaces = os.networkInterfaces();
    for (const name of Object.keys(interfaces)) {
        for (const iface of interfaces[name]) {
            if (iface.family === 'IPv4' && !iface.internal) {
                if (iface.address.startsWith('192.168.') || iface.address.startsWith('10.') || iface.address.startsWith('172.')) {
                    return iface.address;
                }
            }
        }
    }
    return '127.0.0.1';
}

const LOCAL_IP = getLocalIP();
const HTTP_PORT = 3000;
const HTTPS_PORT = 3443;

// Ensure SSL Certificate exists for HTTPS
const certDir = path.join(__dirname, '.cert');
const certPath = path.join(certDir, 'cert.pem');
const keyPath = path.join(certDir, 'key.pem');

if (!fs.existsSync(certDir)) {
    fs.mkdirSync(certDir, { recursive: true });
}

let sslOptions;
if (fs.existsSync(certPath) && fs.existsSync(keyPath)) {
    sslOptions = {
        cert: fs.readFileSync(certPath),
        key: fs.readFileSync(keyPath)
    };
} else {
    console.log('[Victorious Hub] Generating self-signed SSL certificate for local HTTPS...');
    const attrs = [{ name: 'commonName', value: LOCAL_IP }];
    const pems = selfsigned.generate(attrs, {
        days: 3650,
        algorithm: 'sha256',
        keySize: 2048,
        extensions: [
            { name: 'basicConstraints', cA: true },
            { name: 'subjectAltName', altNames: [
                { type: 2, value: 'localhost' },
                { type: 7, ip: '127.0.0.1' },
                { type: 7, ip: LOCAL_IP }
            ]}
        ]
    });
    fs.writeFileSync(certPath, pems.cert);
    fs.writeFileSync(keyPath, pems.private);
    sslOptions = { cert: pems.cert, key: pems.private };
}

// Assets Directory for Church Fliers
const assetsDir = path.join(__dirname, 'public', 'assets');
if (!fs.existsSync(assetsDir)) fs.mkdirSync(assetsDir, { recursive: true });

const storage = multer.diskStorage({
    destination: (req, file, cb) => cb(null, assetsDir),
    filename: (req, file, cb) => {
        const ext = path.extname(file.originalname).toLowerCase() || '.jpg';
        cb(null, 'church_flier' + ext);
    }
});
const upload = multer({ storage });

// High-Resolution Church Service Photo Storage
const photosDir = path.join(__dirname, 'photos');
if (!fs.existsSync(photosDir)) fs.mkdirSync(photosDir, { recursive: true });
app.use('/photos', express.static(photosDir));

const photoStorage = multer.diskStorage({
    destination: (req, file, cb) => cb(null, photosDir),
    filename: (req, file, cb) => {
        const camId = req.query.camId || req.body.camId || '1';
        const d = new Date();
        const dateStr = d.toISOString().replace(/[:.]/g, '-').slice(0, 19);
        const ext = path.extname(file.originalname).toLowerCase() || '.jpg';
        cb(null, `Sunday_Church_Photo_Cam${camId}_${dateStr}${ext}`);
    }
});
const photoUpload = multer({ storage: photoStorage });

let isEmergencyCut = false;
let currentProgramCamId = null; // Master Program starts safely on Sunday Church Flier until Director cuts live

// Get Active Flier URL (Custom uploaded or Default SVG)
function getActiveFlierUrl() {
    try {
        const files = fs.readdirSync(assetsDir);
        const flierFile = files.find(f => f.startsWith('church_flier.'));
        if (flierFile) {
            return `/assets/${flierFile}?v=${Date.now()}`;
        }
    } catch (e) {}
    return '/assets/default_flier.svg';
}

// Global Camera State
const cameras = {
    1: { id: 1, name: 'Pastor / Pulpit', connected: false, tally: 'OFF_AIR', battery: null, charging: null, ws: null, lastSeen: null },
    2: { id: 2, name: 'Choir / Altar', connected: false, tally: 'OFF_AIR', battery: null, charging: null, ws: null, lastSeen: null },
    3: { id: 3, name: 'Congregation / Wide', connected: false, tally: 'OFF_AIR', battery: null, charging: null, ws: null, lastSeen: null },
    4: { id: 4, name: 'Mobile / Roaming 1', connected: false, tally: 'OFF_AIR', battery: null, charging: null, ws: null, lastSeen: null },
    5: { id: 5, name: 'Mobile / Roaming 2', connected: false, tally: 'OFF_AIR', battery: null, charging: null, ws: null, lastSeen: null },
    6: { id: 6, name: 'Guest / Overflow', connected: false, tally: 'OFF_AIR', battery: null, charging: null, ws: null, lastSeen: null }
};

// API: Server Info & Status (Dynamic IP detection for any Wi-Fi)
app.get('/api/info', async (req, res) => {
    const currentIP = getLocalIP();
    const urls = {};
    for (let i = 1; i <= 6; i++) {
        const camUrl = `https://${currentIP}:${HTTPS_PORT}/cam.html?id=${i}`;
        urls[i] = {
            camUrl,
            obsUrl: `http://localhost:${HTTP_PORT}/obs.html?id=${i}`,
            qrCode: await qrcode.toDataURL(camUrl, { width: 220, margin: 1 })
        };
    }
    res.json({
        name: 'Victorious Streaming Hub',
        localIp: currentIP,
        httpPort: HTTP_PORT,
        httpsPort: HTTPS_PORT,
        cameras,
        urls,
        programUrl: `http://localhost:${HTTP_PORT}/obs.html?id=program`,
        flierUrl: getActiveFlierUrl(),
        isEmergencyCut,
        currentProgramCamId
    });
});

// API: Upload Sunday Church Flier
app.post('/api/upload-flier', upload.single('flier'), (req, res) => {
    if (!req.file) return res.status(400).json({ error: 'No file uploaded' });
    const flierUrl = getActiveFlierUrl();
    console.log(`[Victorious Hub] New Church Flier Uploaded: ${req.file.filename}`);
    broadcastToAll({ type: 'flier_updated', url: flierUrl });
    res.json({ success: true, url: flierUrl });
});

// API: Flier Status
app.get('/api/flier-status', (req, res) => {
    res.json({
        url: getActiveFlierUrl(),
        isEmergencyCut
    });
});

// API: Toggle Panic Emergency Cut
// API: Graceful Shutdown Endpoint for Standalone App
app.post('/api/shutdown', (req, res) => {
    res.json({ success: true, message: 'Studio engine shutting down gracefully.' });
    console.log('[Victorious Hub] Studio exit requested. Shutting down gracefully...');
    setTimeout(() => {
        process.exit(0);
    }, 500);
});
app.post('/api/emergency-cut', (req, res) => {
    const { active } = req.body;
    isEmergencyCut = typeof active === 'boolean' ? active : !isEmergencyCut;
    console.log(`[Emergency Slate] Cut state: ${isEmergencyCut ? 'ACTIVE' : 'RESTORED'}`);
    broadcastToAll({ type: 'emergency_cut', active: isEmergencyCut, url: getActiveFlierUrl() });
    res.json({ success: true, isEmergencyCut });
});

// API: Upload Captured Photo from Camera Phone or Director Laptop
app.post('/api/upload-photo', photoUpload.single('photo'), (req, res) => {
    if (!req.file) return res.status(400).json({ error: 'No photo uploaded' });
    const camId = parseInt(req.query.camId || req.body.camId || '1', 10);
    const photoUrl = `/photos/${req.file.filename}`;
    console.log(`[Photo Captured] Camera ${camId} saved: ${req.file.filename}`);
    broadcastToAll({
        type: 'photo_captured',
        camId,
        filename: req.file.filename,
        url: photoUrl,
        timestamp: Date.now()
    });
    res.json({ success: true, url: photoUrl, filename: req.file.filename });
});

// API: List All Saved Church Photos
app.get('/api/photos', (req, res) => {
    try {
        const files = fs.readdirSync(photosDir);
        const photos = files.filter(f => f.match(/\.(jpg|jpeg|png)$/i)).map(f => {
            const stat = fs.statSync(path.join(photosDir, f));
            const match = f.match(/Cam(\d+)/i);
            const camId = match ? parseInt(match[1], 10) : 1;
            const camName = cameras[camId] ? `${cameras[camId].name} (Cam ${camId})` : `Camera ${camId}`;
            return {
                filename: f,
                url: `/photos/${f}`,
                time: stat.mtimeMs,
                createdAt: stat.birthtime || stat.mtime,
                size: stat.size,
                sizeFormatted: (stat.size / 1024 / 1024).toFixed(2) + ' MB',
                camId,
                cameraName: camName
            };
        }).sort((a, b) => b.time - a.time);
        res.json(photos);
    } catch (e) {
        res.json([]);
    }
});

// API: Delete a Saved Church Photo
app.delete('/api/photos/:filename', (req, res) => {
    try {
        const filename = path.basename(req.params.filename);
        const filePath = path.join(photosDir, filename);
        if (fs.existsSync(filePath)) {
            fs.unlinkSync(filePath);
            console.log(`[Photo Deleted] Removed from laptop: ${filename}`);
            broadcastToAll({ type: 'photo_deleted', filename });
            return res.json({ success: true, filename });
        } else {
            return res.status(404).json({ error: 'Photo not found' });
        }
    } catch (e) {
        return res.status(500).json({ error: e.message });
    }
});

// API: Set Tally Manually

// =====================================================================
// OFFLINE BIBLE ENGINE & CANONICAL BOUNDARY GUARD
// =====================================================================
let offlineKjv = null;
try {
    offlineKjv = require('bible-kjv');
    console.log('[Offline Bible] bible-kjv module loaded successfully (31,102 verses ready offline)!');
} catch (e) {
    console.warn('[Offline Bible] bible-kjv failed to load:', e.message);
}

const bibleStructurePath = path.join(__dirname, 'public', 'data', 'bible-structure.json');
let bibleStructure = null;
if (fs.existsSync(bibleStructurePath)) {
    try {
        bibleStructure = JSON.parse(fs.readFileSync(bibleStructurePath, 'utf8'));
    } catch(e) {
        console.warn('[Offline Bible] Failed to parse bible-structure.json:', e.message);
    }
}

app.get('/api/bible/structure', (req, res) => {
    if (bibleStructure) {
        return res.json(bibleStructure);
    }
    res.status(500).json({ error: 'Structure not loaded' });
});

app.get('/api/bible/verse', (req, res) => {
    let { book, chapter, verse, version } = req.query;
    if (!book || !chapter || !verse) {
        return res.status(400).json({ success: false, error: 'Missing book, chapter, or verse' });
    }

    chapter = parseInt(chapter, 10);
    verse = parseInt(verse, 10);
    version = (version || 'KJV').toUpperCase();

    if (!bibleStructure) {
        return res.status(500).json({ success: false, error: 'Bible structure not ready' });
    }

    const cleanBook = book.toLowerCase().replace(/[^a-z0-9]/g, '');
    const bookId = bibleStructure.aliases[cleanBook] || bibleStructure.aliases[book.toLowerCase()];
    if (!bookId) {
        return res.json({ success: false, error: `Unknown book name: "${book}". Please check spelling.` });
    }

    const bookData = bibleStructure.books[bookId];
    if (chapter < 1 || chapter > bookData.totalChapters) {
        return res.json({
            success: false,
            outOfBounds: true,
            maxChapters: bookData.totalChapters,
            error: `${bookData.name} has only ${bookData.totalChapters} chapters.`
        });
    }

    const maxVerses = bookData.versesPerChapter[chapter - 1];
    if (verse < 1 || verse > maxVerses) {
        return res.json({
            success: false,
            outOfBounds: true,
            bookName: bookData.name,
            chapter,
            verse,
            maxVerses,
            error: `${bookData.name} chapter ${chapter} only has ${maxVerses} verses (Verse 1 to ${maxVerses}).`
        });
    }

    const canonicalRef = `${bookData.name.toUpperCase()} ${chapter}:${verse}`;

    // 100% Offline KJV text resolution
    if (offlineKjv && (version === 'KJV' || version === 'OFFLINE')) {
        try {
            let rawText = offlineKjv.getVerse(bookId, chapter, verse);
            if (rawText) {
                let cleanText = rawText.replace(/<[^>]+>/g, '').replace(/\s+/g, ' ').trim();
                return res.json({
                    success: true,
                    ref: canonicalRef,
                    text: cleanText,
                    version: 'KJV',
                    maxVerses,
                    bookId,
                    chapter,
                    verse,
                    isOffline: true
                });
            }
        } catch(e) {
            console.warn('[Offline Bible Read Error]:', e.message);
        }
    }

    // Return structure metadata for other versions
    return res.json({
        success: true,
        ref: canonicalRef,
        bookId,
        bookName: bookData.name,
        chapter,
        verse,
        maxVerses,
        version
    });
});

app.post('/api/tally', (req, res) => {
    const { camId, state } = req.body;
    const targetId = parseInt(camId, 10);
    if (cameras[targetId]) {
        if (state === 'PROGRAM') {
            if (isEmergencyCut) {
                isEmergencyCut = false;
                console.log(`[Emergency Disengaged] Cutting Camera ${targetId} live auto-cleared emergency cut`);
                broadcastToAll({ type: 'emergency_cut', active: false, url: getActiveFlierUrl() });
            }
            currentProgramCamId = targetId;
            for (let id = 1; id <= 6; id++) {
                if (id === targetId) {
                    cameras[id].tally = 'PROGRAM';
                    broadcastTally(id, 'PROGRAM');
                } else {
                    const nextState = cameras[id].connected ? 'STANDBY' : 'OFF_AIR';
                    cameras[id].tally = nextState;
                    broadcastTally(id, nextState);
                }
            }
            broadcastToProgramReceivers(targetId);
            switchOBSScene(targetId, true);
        } else {
            cameras[targetId].tally = state;
            broadcastTally(targetId, state);
        }
        res.json({ success: true, camId: targetId, state });
    } else {
        res.status(404).json({ error: 'Camera not found' });
    }
});


// Create HTTP & HTTPS Servers
const httpServer = http.createServer(app);
const httpsServer = https.createServer(sslOptions, app);

// WebSocket Servers for Signaling & Telemetry
const wssHttp = new WebSocketServer({ server: httpServer });
const wssHttps = new WebSocketServer({ server: httpsServer });

function triggerOffersForCamera(camId, broadcasterWs) {
    if (!broadcasterWs || broadcasterWs.readyState !== WebSocket.OPEN) return;
    if (cameras[camId] && cameras[camId].suspended) return;
    [wssHttp, wssHttps].forEach(wss => {
        wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                // Strict Broadcast Rule: Program OBS is ONLY prompted if this camera is actively cut to PROGRAM
                if (client.isProgramReceiver && currentProgramCamId === camId && cameras[camId] && cameras[camId].tally === 'PROGRAM' && client.receiverId) {
                    console.log(`[Broadcaster Connect] Prompting Camera ${camId} for active Program OBS (${client.receiverId})`);
                    broadcasterWs.send(JSON.stringify({ type: 'receiver_ready', camId, receiverId: client.receiverId }));
                } else if (client.role === 'receiver' && client.camId === camId && client.receiverId) {
                    if (cameras[camId] && cameras[camId].tally === 'PROGRAM') {
                        console.log(`[Broadcaster Connect] Prompting Camera ${camId} for active OBS receiver (${client.receiverId})`);
                        broadcasterWs.send(JSON.stringify({ type: 'receiver_ready', camId, receiverId: client.receiverId }));
                    }
                } else if (client.role === 'director') {
                    if (client.receiverIds) {
                        client.receiverIds.forEach(recId => {
                            if (recId.startsWith(`director_${camId}_`)) {
                                broadcasterWs.send(JSON.stringify({ type: 'receiver_ready', camId, receiverId: recId }));
                            }
                        });
                    }
                }
            }
        });
    });
}

function setupWebSocket(wss) {
    wss.on('connection', (ws, req) => {
        let peerRole = null; // 'broadcaster', 'receiver', 'director'
        let peerCamId = null;

        ws.isAlive = true;
        ws.on('pong', () => { ws.isAlive = true; });

        ws.on('message', (message) => {
            try {
                const data = JSON.parse(message);
                
                // Registration
                if (data.type === 'register') {
                    peerRole = data.role;
                    peerCamId = data.camId === 'program' ? 'program' : parseInt(data.camId, 10);
                    ws.role = peerRole;
                    ws.camId = peerCamId;
                    ws.receiverId = data.receiverId || null;
                    ws.receiverIds = new Set();
                    if (data.receiverId) ws.receiverIds.add(data.receiverId);
                    ws.isProgramReceiver = (data.camId === 'program' || data.role === 'program_receiver');

                    if (ws.isProgramReceiver) {
                        const isLiveProgramActive = (currentProgramCamId && cameras[currentProgramCamId] && cameras[currentProgramCamId].connected && cameras[currentProgramCamId].tally === 'PROGRAM');
                        console.log(`[Master Program OBS Registered] (ID: ${data.receiverId}), current live is Cam ${isLiveProgramActive ? currentProgramCamId : 'NONE (Flier Active)'}`);
                        ws.send(JSON.stringify({ type: 'program_cut', camId: isLiveProgramActive ? currentProgramCamId : null }));
                        if (isLiveProgramActive && cameras[currentProgramCamId].ws && cameras[currentProgramCamId].ws.readyState === WebSocket.OPEN) {
                            cameras[currentProgramCamId].ws.send(JSON.stringify({
                                type: 'receiver_ready',
                                camId: currentProgramCamId,
                                receiverId: data.receiverId
                            }));
                        }
                        return;
                    }

                    if (peerRole === 'broadcaster' && cameras[peerCamId]) {
                        // Check if another phone is already connected to this camera channel
                        if (cameras[peerCamId].connected && cameras[peerCamId].ws && cameras[peerCamId].ws !== ws && cameras[peerCamId].ws.readyState === WebSocket.OPEN) {
                            console.warn(`[Channel Conflict] A second phone tried to connect to Camera ${peerCamId} which is already active!`);
                            ws.send(JSON.stringify({
                                type: 'channel_busy',
                                camId: peerCamId,
                                camName: cameras[peerCamId].name,
                                message: `Camera ${peerCamId} (${cameras[peerCamId].name}) is already in use by another phone.`
                            }));
                            return;
                        }

                        cameras[peerCamId].connected = true;
                        cameras[peerCamId].suspended = false;
                        cameras[peerCamId].ws = ws;
                        cameras[peerCamId].lastSeen = Date.now();
                        // DIRECTOR GATEKEEPER RULE: All newly connected/reconnecting phones enter STANDBY
                        cameras[peerCamId].tally = 'STANDBY';
                        console.log(`[Broadcaster Connected] Camera ${peerCamId} (${cameras[peerCamId].name}) -> Standby (Director Preview Only)`);
                        broadcastToDirectors({ type: 'camera_update', cameras });
                        ws.send(JSON.stringify({ type: 'tally', state: 'STANDBY' }));

                        // Immediately prompt broadcaster to create offers for Director preview only
                        triggerOffersForCamera(peerCamId, ws);

                        // Notify any waiting OBS receivers or director previews that this camera is online!
                        broadcastToReceivers(peerCamId, { type: 'broadcaster_online', camId: peerCamId });
                    }

                    if (peerRole === 'receiver' && peerCamId) {
                        const isCamLive = (cameras[peerCamId] && cameras[peerCamId].connected && cameras[peerCamId].tally === 'PROGRAM');
                        console.log(`[OBS Receiver Registered] For Camera ${peerCamId} (ID: ${data.receiverId}), isLive=${isCamLive}`);

                        // Only prompt broadcaster if camera is actually cut to PROGRAM!
                        if (isCamLive && cameras[peerCamId].ws && cameras[peerCamId].ws.readyState === WebSocket.OPEN) {
                            cameras[peerCamId].ws.send(JSON.stringify({
                                type: 'receiver_ready',
                                camId: peerCamId,
                                receiverId: data.receiverId
                            }));
                        }
                    }
                    return;
                }

                // Operator phone screen locked or app minimized
                if (data.type === 'camera_suspended') {
                    const targetId = parseInt(data.camId || peerCamId, 10);
                    if (cameras[targetId]) {
                        cameras[targetId].connected = false;
                        cameras[targetId].suspended = true;
                        // Strip PROGRAM authority if this camera was live
                        if (currentProgramCamId === targetId || cameras[targetId].tally === 'PROGRAM') {
                            console.log(`[Program Authority Stripped] Camera ${targetId} was LIVE but suspended -> Resetting Program to Flier`);
                            currentProgramCamId = null;
                            cameras[targetId].tally = 'OFF_AIR';
                            broadcastToProgramReceivers(null);
                        } else {
                            cameras[targetId].tally = 'OFF_AIR';
                        }
                        console.log(`[Broadcaster Suspended] Camera ${targetId} screen locked or backgrounded -> Instant failover`);
                        broadcastToDirectors({ type: 'camera_update', cameras });
                        broadcastToReceivers(targetId, { type: 'broadcaster_suspended', camId: targetId });
                    }
                    return;
                }

                // Operator phone screen unlocked or app foregrounded
                if (data.type === 'camera_resumed') {
                    const targetId = parseInt(data.camId || peerCamId, 10);
                    if (cameras[targetId]) {
                        cameras[targetId].connected = true;
                        cameras[targetId].suspended = false;
                        cameras[targetId].ws = ws;
                        cameras[targetId].lastSeen = Date.now();
                        // Waking camera stays in STANDBY until Director explicitly cuts live!
                        cameras[targetId].tally = 'STANDBY';
                        console.log(`[Broadcaster Resumed] Camera ${targetId} restored in STANDBY (Awaiting Director Cut Live)`);
                        broadcastToDirectors({ type: 'camera_update', cameras });
                        ws.send(JSON.stringify({ type: 'tally', state: 'STANDBY' }));
                        broadcastToReceivers(targetId, { type: 'broadcaster_online', camId: targetId });
                        triggerOffersForCamera(targetId, ws);
                    }
                    return;
                }

                // Battery & Telemetry from Phone
                if (data.type === 'telemetry') {
                    if (peerCamId && cameras[peerCamId]) {
                        cameras[peerCamId].battery = data.battery;
                        cameras[peerCamId].charging = data.charging;
                        cameras[peerCamId].lastSeen = Date.now();
                        broadcastToDirectors({ type: 'telemetry_update', camId: peerCamId, battery: data.battery, charging: data.charging });
                    }
                    return;
                }

                // Cameraman Voluntary Handoff to Church Flier Slate
                if (data.type === 'cameraman_handoff') {
                    const targetId = parseInt(data.camId || peerCamId, 10);
                    if (cameras[targetId]) {
                        console.log(`[Cameraman Handoff] Camera ${targetId} (${cameras[targetId].name}) voluntarily yielded live broadcast to Church Flier`);
                        if (currentProgramCamId === targetId || cameras[targetId].tally === 'PROGRAM') {
                            currentProgramCamId = null;
                            cameras[targetId].tally = 'STANDBY';
                            broadcastTally(targetId, 'STANDBY');
                            broadcastToProgramReceivers(null);
                        } else {
                            cameras[targetId].tally = 'STANDBY';
                            broadcastTally(targetId, 'STANDBY');
                        }
                        broadcastToDirectors({
                            type: 'camera_update',
                            cameras,
                            notification: `ðŸ•Šï¸ Camera ${targetId} (${cameras[targetId].name}) handed off live broadcast to Church Flier.`
                        });
                        broadcastToReceivers(targetId, { type: 'tally_update', camId: targetId, state: 'STANDBY' });
                    }
                    return;
                }

                if (data.type === 'receiver_ready' && data.receiverId) {
                    if (!ws.receiverIds) ws.receiverIds = new Set();
                    ws.receiverIds.add(data.receiverId);
                }

                // Intercom Audio Alerts & Crew Pings (Targeted / Isolated to specific Camera or All)
                if (data.type === 'intercom_ping') {
                    const target = data.target || 'all';
                    [wssHttp, wssHttps].forEach(wss => {
                        wss.clients.forEach(client => {
                            if (client.readyState === WebSocket.OPEN && client.role === 'broadcaster') {
                                if (target === 'all' || client.camId === parseInt(target, 10)) {
                                    client.send(JSON.stringify(data));
                                }
                            }
                        });
                    });
                    return;
                }

                // Live Broadcast Graphics & Lower-Thirds Overlay Commands (OBS Layer)
                if (['overlay_show', 'overlay_hide'].includes(data.type)) {
                    broadcastToAll(data);
                    return;
                }

                // Intercom Talkback: Director speaking to all or specific camera phone
                if (data.type === 'intercom_active') {
                    [wssHttp, wssHttps].forEach(wss => {
                        wss.clients.forEach(client => {
                            if (client.readyState === WebSocket.OPEN && client.role === 'broadcaster') {
                                if (data.target === 'all' || client.camId === parseInt(data.target, 10)) {
                                    client.send(JSON.stringify(data));
                                }
                            }
                        });
                    });
                    return;
                }

                // WebRTC Signaling: Forward receiver_ready, offer, answer, ice-candidate
                if (['receiver_ready', 'offer', 'answer', 'ice-candidate', 'intercom_offer', 'intercom_answer', 'intercom_ice'].includes(data.type)) {
                    let targetCamId = parseInt(data.camId || ws.camId, 10);
                    if (isNaN(targetCamId) || data.camId === 'program') {
                        targetCamId = currentProgramCamId;
                    }
                    [wssHttp, wssHttps].forEach(wss => {
                        wss.clients.forEach(client => {
                            if (client !== ws && client.readyState === WebSocket.OPEN) {
                                if (ws.role === 'broadcaster') {
                                    // Broadcaster sending to specific receiver (OBS or specific Director session)
                                    if (data.directorId && client.role === 'director') {
                                        client.send(JSON.stringify(data));
                                    } else if (data.receiverId) {
                                        const matchesDirect = (client.receiverId === data.receiverId);
                                        const matchesSet = (client.receiverIds && client.receiverIds.has(data.receiverId));
                                        if (matchesDirect || matchesSet) {
                                            client.send(JSON.stringify(data));
                                        }
                                    } else if (client.camId === targetCamId || client.isProgramReceiver) {
                                        client.send(JSON.stringify(data));
                                    }
                                } else {
                                    // Receiver or Director sending to Broadcaster
                                    if (client.role === 'broadcaster' && client.camId === targetCamId) {
                                        client.send(JSON.stringify(data));
                                    }
                                }
                            }
                        });
                    });
                    return;
                }

                // Director Tally Change & OBS Live Scene Switching (Exclusive Single Live Rule)
                if (data.type === 'set_tally') {
                    const { camId, state } = data;
                    const targetId = parseInt(camId, 10);
                    if (cameras[targetId]) {
                        if (state === 'PROGRAM') {
                            if (isEmergencyCut) {
                                isEmergencyCut = false;
                                console.log(`[Emergency Disengaged] Cutting Camera ${targetId} live auto-cleared emergency cut`);
                                broadcastToAll({ type: 'emergency_cut', active: false, url: getActiveFlierUrl() });
                            }
                            currentProgramCamId = targetId;
                            // EXCLUSIVE PROGRAM RULE: Only ONE camera can be LIVE on air!
                            for (let id = 1; id <= 6; id++) {
                                if (id === targetId) {
                                    cameras[id].tally = 'PROGRAM';
                                    broadcastTally(id, 'PROGRAM');
                                } else {
                                    const nextState = cameras[id].connected ? 'STANDBY' : 'OFF_AIR';
                                    cameras[id].tally = nextState;
                                    broadcastTally(id, nextState);
                                }
                            }
                            broadcastToProgramReceivers(targetId);
                            switchOBSScene(targetId, true);
                        } else if (state === 'PREVIEW') {
                            // Set Preview on this camera without cutting Program
                            cameras[targetId].tally = 'PREVIEW';
                            broadcastTally(targetId, 'PREVIEW');
                            switchOBSScene(targetId, false);
                        } else {
                            cameras[targetId].tally = state;
                            broadcastTally(targetId, state);
                        }
                    }
                }

            } catch (err) {
                console.error('[WS Parse Error]:', err);
            }
        });

        ws.on('close', () => {
            if (peerRole === 'broadcaster' && peerCamId && cameras[peerCamId]) {
                if (cameras[peerCamId].ws === ws) {
                    cameras[peerCamId].connected = false;
                    cameras[peerCamId].suspended = false;
                    cameras[peerCamId].ws = null;
                    if (currentProgramCamId === peerCamId || cameras[peerCamId].tally === 'PROGRAM') {
                        console.log(`[Program Authority Stripped] Camera ${peerCamId} disconnected -> Resetting Program to Flier`);
                        currentProgramCamId = null;
                        cameras[peerCamId].tally = 'OFF_AIR';
                        broadcastToProgramReceivers(null);
                    } else {
                        cameras[peerCamId].tally = 'OFF_AIR';
                    }
                    console.log(`[Broadcaster Disconnected] Camera ${peerCamId}`);
                    broadcastToDirectors({ type: 'camera_update', cameras });
                    broadcastToReceivers(peerCamId, { type: 'broadcaster_offline', camId: peerCamId });
                }
            }
        });
    });
}

// Reliable WebSocket Heartbeat (Ping every 4000ms; terminates dead sockets cleanly)
setInterval(() => {
    [wssHttp, wssHttps].forEach(wss => {
        wss.clients.forEach(ws => {
            if (ws.isAlive === false) {
                console.warn(`[Heartbeat Timeout] Terminating inactive socket for role=${ws.role}, camId=${ws.camId}`);
                return ws.terminate();
            }
            ws.isAlive = false;
            try { ws.ping(); } catch (e) {}
        });
    });
}, 4000);

setupWebSocket(wssHttp);
setupWebSocket(wssHttps);

// Broadcast signaling messages between broadcaster and receiver for a camera
function broadcastSignaling(senderWs, data) {
    const targetCamId = senderWs.camId;
    const targetRole = senderWs.role === 'broadcaster' ? 'receiver' : 'broadcaster';

    [wssHttp, wssHttps].forEach(wss => {
        wss.clients.forEach(client => {
            if (client !== senderWs && client.readyState === WebSocket.OPEN && client.camId === targetCamId) {
                if (client.role === targetRole || client.role === 'director') {
                    client.send(JSON.stringify(data));
                }
            }
        });
    });
}

// Broadcast to Master Program receivers (Cut Live on ?id=program)
function broadcastToProgramReceivers(targetCamId) {
    const msg = JSON.stringify({ type: 'program_cut', camId: targetCamId });
    [wssHttp, wssHttps].forEach(wss => {
        wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN && client.isProgramReceiver) {
                client.send(msg);
                if (cameras[targetCamId] && cameras[targetCamId].connected && cameras[targetCamId].ws && cameras[targetCamId].ws.readyState === WebSocket.OPEN) {
                    cameras[targetCamId].ws.send(JSON.stringify({
                        type: 'receiver_ready',
                        camId: targetCamId,
                        receiverId: client.receiverId
                    }));
                }
            }
        });
    });
}

// Broadcast to OBS receivers for a specific camera
function broadcastToReceivers(camId, payload) {
    const msg = JSON.stringify(payload);
    [wssHttp, wssHttps].forEach(wss => {
        wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN && (client.role === 'receiver' || client.role === 'director' || client.isProgramReceiver)) {
                if (client.role === 'director' || client.isProgramReceiver || client.camId === parseInt(camId, 10)) {
                    client.send(msg);
                }
            }
        });
    });
}

// Broadcast Tally State to Phone Broadcaster & Director
function broadcastTally(camId, state) {
    [wssHttp, wssHttps].forEach(wss => {
        wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                if (client.role === 'broadcaster' && client.camId === parseInt(camId, 10)) {
                    client.send(JSON.stringify({ type: 'tally', state }));
                }
                if (client.role === 'director') {
                    client.send(JSON.stringify({ type: 'tally_update', camId, state }));
                }
            }
        });
    });
}

// Broadcast updates to Director dashboards
function broadcastToDirectors(payload) {
    const msg = JSON.stringify(payload);
    [wssHttp, wssHttps].forEach(wss => {
        wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN && client.role === 'director') {
                client.send(msg);
            }
        });
    });
}

// Broadcast to all connected clients (Phones, OBS, and Directors)
function broadcastToAll(payload) {
    const msg = JSON.stringify(payload);
    [wssHttp, wssHttps].forEach(wss => {
        wss.clients.forEach(client => {
            if (client.readyState === WebSocket.OPEN) {
                client.send(msg);
            }
        });
    });
}

// Optional: OBS WebSocket Auto-Connect & Scene Switching
let obsClient = null;
let isObsConnected = false;

// Function to switch scene in OBS Studio from Director Dashboard
async function switchOBSScene(camId, isProgram = true) {
    if (!obsClient || !isObsConnected) return;
    try {
        const { scenes } = await obsClient.call('GetSceneList');
        const cam = cameras[camId];
        const camKeywords = [`cam ${camId}`, `cam${camId}`, `camera ${camId}`, `camera${camId}`, cam.name.toLowerCase().split(' ')[0]];
        
        const matching = scenes.find(s => {
            const name = s.sceneName.toLowerCase();
            return camKeywords.some(kw => name.includes(kw));
        });

        if (matching) {
            if (isProgram) {
                await obsClient.call('SetCurrentProgramScene', { sceneName: matching.sceneName });
                console.log(`[OBS Switcher] Switched Live Program to Scene: "${matching.sceneName}"`);
            } else {
                await obsClient.call('SetCurrentPreviewScene', { sceneName: matching.sceneName });
                console.log(`[OBS Switcher] Switched Preview to Scene: "${matching.sceneName}"`);
            }
        }
    } catch (e) {
        console.warn('[OBS Switch Error]:', e.message);
    }
}

try {
    const { OBSWebSocket } = require('obs-websocket-js');
    obsClient = new OBSWebSocket();
    
    async function connectOBS() {
        try {
            await obsClient.connect('ws://localhost:4455');
            isObsConnected = true;
            console.log('[Victorious Hub] Successfully connected to OBS Studio WebSocket (Port 4455)!');
            broadcastToDirectors({ type: 'obs_status', connected: true });

            obsClient.on('CurrentProgramSceneChanged', (event) => {
                const sceneName = (event.sceneName || '');
                console.log(`[OBS Scene Sync] Active scene in OBS Studio: "${sceneName}" (Director Command Center remains SSOT for tallies)`);
            });

            obsClient.on('ConnectionClosed', () => {
                isObsConnected = false;
                console.log('[OBS WebSocket] Connection closed, will retry in 5s...');
                broadcastToDirectors({ type: 'obs_status', connected: false });
                setTimeout(connectOBS, 5000);
            });
        } catch (e) {
            // OBS not running yet, retry in background
            setTimeout(connectOBS, 8000);
        }
    }
    connectOBS();
} catch (e) {
    console.log('[Victorious Hub] OBS WebSocket client module ready for live linking.');
}

// Start Listening
httpServer.listen(HTTP_PORT, () => {
    console.log(`\n=============================================================`);
    console.log(`   VICTORIOUS STREAMING HUB - CHURCH BROADCAST STUDIO       `);
    console.log(`=============================================================`);
    console.log(`[Director Dashboard]  http://localhost:${HTTP_PORT}`);
    console.log(`[Mobile Cameras URL]  https://${LOCAL_IP}:${HTTPS_PORT}/cam.html?id=1`);
    console.log(`[OBS Browser Source]  http://localhost:${HTTP_PORT}/obs.html?id=1`);
    console.log(`=============================================================\n`);
});

httpsServer.listen(HTTPS_PORT);
