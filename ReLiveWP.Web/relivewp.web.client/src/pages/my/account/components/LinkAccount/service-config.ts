export type Stage = 'service' | 'handle' | 'credentials' | 'loading' | 'redirect' | 'configure' | 'applying' | 'done' | 'error';

type ServiceConfig = {
    title: string;
    placeholder: string;
};

type CredentialServiceConfig = {
    urlTitle: string;
    urlPlaceholder: string;
    usernameTitle: string;
    secretTitle: string;
    labelTitle: string;
    labelPlaceholder: string;
};

const SERVICES: Partial<Record<string, ServiceConfig>> = {
    mastodon: { title: "ActivityPub Handle", placeholder: "@wamwoowam@snug.moe" },
    misskey: { title: "ActivityPub Handle", placeholder: "@wamwoowam@snug.moe" },
    atproto: { title: "Bluesky Handle", placeholder: "@wamwoowam.co.uk" },
};

const CREDENTIAL_SERVICES: Partial<Record<string, CredentialServiceConfig>> = {
    webdav: {
        urlTitle: "Server address",
        urlPlaceholder: "https://cloud.example.com/remote.php/dav/files/me/",
        usernameTitle: "Username",
        secretTitle: "Password",
        labelTitle: "Name (optional)",
        labelPlaceholder: "my fancy webdav server",
    },
    carddav: {
        urlTitle: "Server address",
        urlPlaceholder: "https://cloud.example.com/remote.php/dav/",
        usernameTitle: "Username",
        secretTitle: "Password",
        labelTitle: "Name (optional)",
        labelPlaceholder: "my address book",
    },
};

export function requiresHandle(service: string): boolean {
    return service in SERVICES;
}

export function requiresCredentials(service: string): boolean {
    return service in CREDENTIAL_SERVICES;
}

export function stageForService(service: string): Stage {
    if (requiresCredentials(service)) return 'credentials';
    return requiresHandle(service) ? 'handle' : 'loading';
}

export function getServiceConfig(service: string): ServiceConfig | undefined {
    return SERVICES[service];
}

export function getCredentialServiceConfig(service: string): CredentialServiceConfig | undefined {
    return CREDENTIAL_SERVICES[service];
}
