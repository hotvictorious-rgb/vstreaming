# 👑 Victorious Streaming Hub

> **Professional Church Multi-Camera Broadcast Studio for OBS Studio**  
> *Architected and Engineered under the leadership of **Victory Saviour Edet**, CEO of VICTORIOUS MARKET.*

---

## 🌟 Overview

**Victorious Streaming Hub** transforms smartphones and tablets into wireless, studio-grade broadcast cameras over local Wi-Fi, feeding ultra-low latency WebRTC video directly into **OBS Studio** for live church services, conferences, and multi-camera productions.

Built with complete independence and zero cloud dependencies, the entire ecosystem runs on your local sanctuary network—giving you broadcast-grade reliability with zero monthly subscription fees.

---

## ✨ Key Features

- **📱 Multi-Camera Wireless Ingest:** Connect multiple smartphones (Pastor/Pulpit, Choir/Altar, Congregation/Wide, Mobile Roaming) instantly using dynamic QR codes.
- **⚡ Ultra-Low Latency WebRTC:** Sub-second (50–150ms) video transmission across local Wi-Fi without cable clutter.
- **🚨 Live Tally Lights & Telemetry:**
  - Real-time **RED ON AIR** and **GREEN STANDBY** tally lights displayed on phone screens so camera operators always know when they are live.
  - Live battery percentage (🔋 52%) and charging indicators (⚡ Charging) streamed back to the Director Command Center.
- **🎙️ Integrated Director-to-Crew Intercom Talkback:**
  - Real-time voice talkback from Director's laptop straight into camera operators' Bluetooth earbuds (or phone loudspeakers) with zero latency.
  - **Push-To-Talk Keyboard Control:** Press and hold **`[T]`** to speak to all cameras; release to instantly mute. (The **`[Spacebar]`** remains dedicated to instant **Emergency Cut to Flier**).
  - **Point-to-Point Private Calling:** Speak to all cameras simultaneously (Partyline) or click **`[🎙️ Talk]`** on an individual camera card to talk privately without distracting other operators.
  - **On-Screen Audio & Visual Cues:** Volume slider and quick mute on mobile screens, plus flashing visual banners (`🎙️ Director Speaking...`) so operators stay alerted even in loud praise and worship.
  - **100% Broadcast Safe:** The intercom channel is completely decoupled from OBS Studio (`obs.html`); talkback audio never leaks into the YouTube or Facebook livestream.
- **📸 High-Resolution Remote Photo Capture:** Snap uncompressed, high-definition service photos directly from any connected mobile camera without interrupting the video feed.
- **🎛 OBS Studio WebSocket 5.x Integration:**
  - Automated Program / Preview camera switching.
  - Emergency 1-click **CUT TO FLIER** button for unexpected technical pauses or service transitions.
- **🛡 Decoupled Broadcast Resilience:**
  - The broadcast pipeline (Phone ➔ OBS Studio ➔ YouTube/Facebook) is completely decoupled from preview screens.
  - Accidental tab or dashboard window closures **never interrupt** the live stream.
  - Built-in eforeunload exit confirmation prevents accidental window closure.
- **🚀 1-Click Portable Deployment:** Zero complicated configuration; copy the folder to any Windows laptop and launch with one click.

---

## 🏗 Recommended Camera Settings

For the smoothest broadcast performance in sanctuary lighting conditions:
- **Frame Rate:** **30 FPS** *(Recommended over 60 FPS to prevent phone thermal overheating, eliminate Wi-Fi packet jitter, and allow 2x more light into camera sensors during worship).*
- **Resolution:** **1080p Full HD** (or 720p HD for roaming mobile cameras on busy networks).
- **Network:** 5 GHz dedicated Wi-Fi router recommended for lowest latency.

---

## 🚀 Quick Start Guide

### 1. First-Time Setup
Double-click Setup-First-Time.bat as Administrator. This will:
- Install all required Node.js dependencies (
pm install).
- Configure Windows Firewall rules for Ports 3000 (HTTP), 3443 (HTTPS), and 4455 (OBS WebSocket).
- Automatically generate desktop shortcuts.

### 2. Launch the Studio
Double-click the desktop shortcut **Victorious Streaming Hub** (or run Launch-Studio.vbs).
- The background server will start automatically.
- The standalone Command Center interface will open immediately.

### 3. Connect Mobile Cameras
1. Ensure your camera smartphones are connected to the same Wi-Fi router.
2. In the Command Center, click **📲 Connect** under any camera slot to reveal the QR code.
3. Open the camera app on the phone and scan the QR code (or type the HTTPS link https://<YOUR-IP>:3443/cam.html?id=1).
4. Accept the local security prompt and tap **Start Camera**. The live video feed will appear instantly on the director multiview screen.

### 4. Connect OBS Studio
1. Open OBS Studio.
2. Enable WebSocket server under **Tools ➔ WebSocket Server Settings** (Port: 4455).
3. Add a **Browser Source** in OBS:
   - **URL:** http://localhost:3000/obs.html?id=program
   - **Width:** 1920
   - **Height:** 1080
   - Check *Shutdown source when not visible* = **Unchecked**.

---

## 📁 Repository Structure

`
├── .cert/                     # Auto-generated local SSL certificates for mobile HTTPS
├── public/                    # Frontend dashboards and camera interfaces
│   ├── assets/                # Church fliers, default graphics, and assets
│   ├── css/                   # Dark-mode broadcast studio styles
│   ├── cam.html               # Mobile camera capture interface (with Tally & Flash)
│   ├── index.html             # Sanctuary Multiview Director Command Center
│   └── obs.html               # Ultra-low latency OBS Studio browser ingest
├── photos/                    # Local storage for high-res remote photos (.gitignore excluded)
├── server.js                  # Core Node.js WebRTC signaling, WebSocket, & Express engine
├── main.js                    # Electron runtime wrapper
├── Launcher.cs                # Native Windows C# launcher source
├── Launch-Studio.vbs          # Zero-flicker 1-click Windows portable launcher
├── Setup-First-Time.bat       # Automatic 1-click system installer & firewall setup
├── Start-Sunday-Studio.bat    # Background service starter
├── Stop-Sunday-Studio.bat     # Clean shutdown script
└── package.json               # Node.js project manifest & dependencies
`

---

## 🔒 Privacy & Security

- **Local Network Isolation:** No camera feeds or audio leave your sanctuary local area network unless streamed via your own OBS Studio to your chosen destination.
- **Excluded Media:** All captured test and live service photos are kept strictly local and excluded from version control via .gitignore.

---

## 📜 Credits & Attribution

Designed, architected, and brought to life under the leadership of **Victory Saviour Edet**, CEO of **VICTORIOUS MARKET**.

Dedicated to excellence in church live broadcasting, media ministry, and high-performance real-time video engineering.
