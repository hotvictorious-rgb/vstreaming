// Victorious Broadcaster - PWA Service Worker
const CACHE_NAME = 'victorious-cam-v1';

self.addEventListener('install', (event) => {
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', (event) => {
    // Live WebRTC and WebSocket streaming bypass service worker caching
    if (event.request.url.includes('/api/') || event.request.url.startsWith('ws') || event.request.method !== 'GET') {
        return;
    }
    // Network-first strategy for dynamic studio updates
    event.respondWith(
        fetch(event.request).catch(() => caches.match(event.request))
    );
});
