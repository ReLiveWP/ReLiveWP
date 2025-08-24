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
    "google_drive": {
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
    "Storage": ["onedrive", "google_drive", "dropbox"]
}

export type AccountInfo = {
    name: string,
    url: string
}

export type Connections = {
    connections: Partial<{ [key in AccountType]: AccountInfo[] | undefined }>
}

export type LinkedAccountsContext = {
    linkedAccounts: Signal<Connections>
}

export const OpenDialogContext = createContext<(service: AccountType) => void>(null);
export const LinkedAccountsContext = createContext<LinkedAccountsContext>(null);
