import { useContext } from "preact/hooks";
import BlueskyIcon from "../icons/bluesky";
import DropboxIcon from "../icons/dropbox";
import GoogleDriveIcon from "../icons/google-drive";
import MastodonIcon from "../icons/mastodon";
import MisskeyIcon from "../icons/misskey";
import OneDriveIcon from "../icons/onedrive";
import WebDavIcon from "../icons/webdav";
import { Signal } from "@preact/signals";
import { createContext, type JSX } from "preact";
import { ServiceCaps } from "~/util/service-caps";
import GooglePhotosIcon from "../icons/google-photos";

export type AccountIcon = (props: { class?: string }) => JSX.Element;

export type AccountTypeEntry = {
    name: string;
    icon: AccountIcon;
    allowsMany: boolean;
    capOptions?: Partial<Record<number, { name?: string, icon?: AccountIcon }>>
};

export const AccountTypes = {
    "atproto": {
        name: "bluesky",
        icon: BlueskyIcon,
        allowsMany: true,
    },
    "mastodon": {
        name: "mastodon",
        icon: MastodonIcon,
        allowsMany: true
    },
    "misskey": {
        name: "misskey",
        icon: MisskeyIcon,
        allowsMany: true,
    },
    "google": {
        name: "google",
        icon: GoogleDriveIcon,
        allowsMany: false,
        capOptions: {
            [ServiceCaps.photoSync]: {
                name: "google photos",
                icon: GooglePhotosIcon
            }
        }
    },
    "microsoft": {
        name: "microsoft onedrive",
        icon: OneDriveIcon,
        allowsMany: false
    },
    "dropbox": {
        name: "dropbox",
        icon: DropboxIcon,
        allowsMany: true
    },
    "webdav": {
        name: "webdav share",
        icon: WebDavIcon,
        allowsMany: true
    }
} satisfies Record<string, AccountTypeEntry>;

export type AccountType = keyof typeof AccountTypes;

export interface CapabilityGroup {
    name: string;
    caps: number;
}

export const CapabilityGroups: CapabilityGroup[] = [
    { name: "Social",       caps: ServiceCaps.socialFeed | ServiceCaps.socialPost | ServiceCaps.socialCheckIn | ServiceCaps.socialNotifications },
    { name: "Mail",         caps: ServiceCaps.email },
    { name: "People",       caps: ServiceCaps.contacts },
    { name: "Calendar",     caps: ServiceCaps.calendar },
    { name: "Messenger",    caps: ServiceCaps.messaging },
    { name: "Photo Sync",   caps: ServiceCaps.photoSync },
    { name: "File Storage", caps: ServiceCaps.fileStorage },
    { name: "Marketplace",  caps: ServiceCaps.marketplaceStream | ServiceCaps.marketplacePurchase },
];

export type AccountInfo = {
    id: string
    name: string
    url: string
    needs_relink: boolean
    enabled_capabilities?: number
}

export type Connections = {
    connections: Partial<{ [key in AccountType]: AccountInfo[] | undefined }>
}

export type AvailableConnectedService = {
    service: string;
    displayName: string;
    capabilities: number;
}

export type LinkedAccountsContext = {
    linkedAccounts: Signal<Connections>
    availableLinks: Signal<AvailableConnectedService[]>
    doRefresh: () => void
}

export type OpenDialogAction =
    | { dialog: 'link'; service: AccountType; initialCaps?: number; existingConnectionId?: string; currentEnabledCaps?: number }
    | { dialog: 'unlink'; service: AccountType; id: string }
    | { dialog: 'relink'; service: AccountType; id: string };

export const OpenDialogContext = createContext<(action: OpenDialogAction) => void>(null!);
export const LinkedAccountsContext = createContext<LinkedAccountsContext>(null!);

export function useOpenDialog() {
    return useContext(OpenDialogContext);
}

export function useLinkedAccounts() {
    return useContext(LinkedAccountsContext);
}
