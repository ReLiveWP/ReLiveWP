import { Signal } from "@preact/signals";

export const updateReady = new Signal(false);

let waiting: ServiceWorker | null = null;

function offer(worker: ServiceWorker | null): void {
    if (worker === null || navigator.serviceWorker.controller === null) return;

    waiting = worker;
    updateReady.value = true;
}

function watch(registration: ServiceWorkerRegistration): void {
    offer(registration.waiting);

    registration.addEventListener("updatefound", () => {
        const installing = registration.installing;
        if (installing === null) return;

        installing.addEventListener("statechange", () => {
            if (installing.state === "installed") offer(installing);
        });
    });
}

export function registerServiceWorker(): void {
    if (!("serviceWorker" in navigator)) return;

    // a worker left behind by a production build would shadow the dev server's assets
    if (process.env.NODE_ENV !== "production") {
        void navigator.serviceWorker.getRegistrations()
            .then((all) => { all.forEach((one) => void one.unregister()); })
            .catch(() => undefined);
        return;
    }

    let reloading = false;
    navigator.serviceWorker.addEventListener("controllerchange", () => {
        if (reloading) return;

        reloading = true;
        window.location.reload();
    });

    window.addEventListener("load", () => {
        void navigator.serviceWorker.register("/sw.js").then(watch).catch(() => undefined);
    });
}

export function applyUpdate(): void {
    waiting?.postMessage("SKIP_WAITING");
    updateReady.value = false;
}
