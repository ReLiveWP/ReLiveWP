export type Stage = 'handle' | 'loading' | 'redirect' | 'configure' | 'applying' | 'done' | 'error';

type ServiceConfig = {
    title: string;
    placeholder: string;
};

const SERVICES: Partial<Record<string, ServiceConfig>> = {
    mastodon: { title: "ActivityPub Handle", placeholder: "@wamwoowam@snug.moe" },
    misskey: { title: "ActivityPub Handle", placeholder: "@wamwoowam@snug.moe" },
    atproto: { title: "Bluesky Handle", placeholder: "@wamwoowam.co.uk" },
};

export function requiresHandle(service: string): boolean {
    return service in SERVICES;
}

export function getServiceConfig(service: string): ServiceConfig | undefined {
    return SERVICES[service];
}
