// =============================================================
// Service Worker de OpeApp
// =============================================================
// Flujo de actualización de datos:
//   1. Editas wwwroot/data/comun.json y/o especifica.json
//   2. Subes el número en wwwroot/data/version.json (v1 -> v2)
//   3. Despliegas
//   4. En el siguiente fetch de la app, este SW detecta el cambio
//      de versión, purga la caché y vuelve a bajar los JSONs.
//   Si solo tocas los JSON pero olvidas subir la versión, los
//      usuarios seguirán viendo los datos antiguos.
// =============================================================

const CACHE_NAME = 'opeapp-data';
const VERSION_URL = '/data/version.json';
const VERSION_CACHE_KEY = '/__cached_version__';
const DATA_URLS = [
    '/data/comun.json',
    '/data/especifica.json',
    '/data/version.json'
];
const VERSION_CHECK_DEBOUNCE_MS = 30_000;

let versionCheckPromise = null;
let lastVersionCheck = 0;

async function checkVersion() {
    const ahora = Date.now();
    if (versionCheckPromise) return versionCheckPromise;
    if (ahora - lastVersionCheck < VERSION_CHECK_DEBOUNCE_MS) return;

    lastVersionCheck = ahora;
    versionCheckPromise = (async () => {
        try {
            // fetch() dentro del SW no es interceptado por el propio SW,
            // va directo a la red. Con cache: 'no-store' nos aseguramos
            // de no recibir una respuesta cacheada por el navegador.
            const response = await fetch(VERSION_URL + '?t=' + Date.now(), { cache: 'no-store' });
            if (!response.ok) return;

            const data = await response.json();
            const serverVersion = data.version;
            if (typeof serverVersion !== 'number') return;

            const cache = await caches.open(CACHE_NAME);
            const cachedVersionResponse = await cache.match(VERSION_CACHE_KEY);
            let cachedVersion = null;
            if (cachedVersionResponse) {
                try {
                    const cached = await cachedVersionResponse.clone().json();
                    cachedVersion = cached.version;
                } catch { }
            }

            if (cachedVersion !== serverVersion) {
                // Si la caché ya estaba inicializada, es un cambio real de versión:
                // la purga entera para forzar re-descarga de los JSONs.
                if (cachedVersion !== null) {
                    await caches.delete(CACHE_NAME);
                }
                const newCache = await caches.open(CACHE_NAME);
                await newCache.put(
                    VERSION_CACHE_KEY,
                    new Response(JSON.stringify({ version: serverVersion }), {
                        headers: { 'Content-Type': 'application/json' }
                    })
                );
            }
        } catch {
            // Sin red: no hacemos nada, se mantiene la caché actual
        } finally {
            versionCheckPromise = null;
        }
    })();

    return versionCheckPromise;
}

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache =>
            Promise.all(
                DATA_URLS.map(u =>
                    cache.add(new Request(u, { cache: 'reload' })).catch(() => { })
                )
            )
        )
    );
    self.skipWaiting();
});

self.addEventListener('activate', event => {
    event.waitUntil((async () => {
        // Limpia cualquier caché de versiones anteriores del SW
        const keys = await caches.keys();
        await Promise.all(
            keys
                .filter(k => k.startsWith('opeapp-data') && k !== CACHE_NAME)
                .map(k => caches.delete(k))
        );
        await self.clients.claim();
    })());
});

self.addEventListener('fetch', event => {
    if (event.request.method !== 'GET') return;

    const url = new URL(event.request.url);
    const esDato = DATA_URLS.some(u => url.pathname.endsWith(u.replace(/^\//, '')));

    if (esDato) {
        event.respondWith(handleDataFetch(event.request));
    }
});

async function handleDataFetch(request) {
    // Dispara la comprobación de versión (no bloquea la respuesta)
    checkVersion();

    const cache = await caches.open(CACHE_NAME);
    const cached = await cache.match(request);

    // Stale-while-revalidate: si hay caché se sirve al instante y
    // en paralelo se pide a la red para mantenerla actualizada.
    const networkFetch = fetch(request).then(response => {
        if (response.ok) {
            cache.put(request, response.clone());
        }
        return response;
    }).catch(() => cached);

    return cached || networkFetch;
}
