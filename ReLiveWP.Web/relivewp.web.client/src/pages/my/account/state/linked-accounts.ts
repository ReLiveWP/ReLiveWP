import { useContext } from "preact/hooks";
import BlueskyIcon from "../icons/bluesky";
import DropboxIcon from "../icons/dropbox";
import GoogleDriveIcon from "../icons/google-drive";
import MastodonIcon from "../icons/mastodon";
import MisskeyIcon from "../icons/misskey";
import OneDriveIcon from "../icons/onedrive";
import { Signal } from "@preact/signals";
import { createContext } from "preact";

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
        name: "google drive",
        icon: GoogleDriveIcon,
        allowsMany: false
    },
    "onedrive": {
        name: "onedrive",
        icon: OneDriveIcon,
        allowsMany: false
    },
    "dropbox": {
        name: "dropbox",
        icon: DropboxIcon,
        allowsMany: true
    }
}

export type AccountType = keyof typeof AccountTypes;

export const AccountTypeGroups: { [key: string]: AccountType[] } = {
    "Social": ["atproto", "mastodon", "misskey"],
    "Storage": ["onedrive", "google", "dropbox"]
}

export type AccountInfo = {
    id: string
    name: string
    url: string
    needs_relink: boolean
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
    | { dialog: 'link'; service: AccountType }
    | { dialog: 'unlink'; service: AccountType; id: string }
    | { dialog: 'relink'; id: string };

export const OpenDialogContext = createContext<(action: OpenDialogAction) => void>(null!);
export const LinkedAccountsContext = createContext<LinkedAccountsContext>(null!);

export function useOpenDialog() {
    return useContext(OpenDialogContext);
}

export function useLinkedAccounts() {
    return useContext(LinkedAccountsContext);
}
