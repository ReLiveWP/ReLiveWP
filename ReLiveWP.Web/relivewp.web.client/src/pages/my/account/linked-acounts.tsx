import { useState } from "preact/hooks";
import { useAppState } from "../../../state/app-state";
import { ENDPOINT_BEGIN_ACCOUNT_LINKING } from "../../../util/endpoints";
import BlueskyIcon from "./icons/bluesky";
import DropboxIcon from "./icons/dropbox";
import GoogleDriveIcon from "./icons/google-drive";
import MastodonIcon from "./icons/mastodon";
import MisskeyIcon from "./icons/misskey";
import OneDriveIcon from "./icons/onedrive";

import "./linked-accounts.scss"
import LinkAccountDialog from "./components/LinkAccountDialog";
import { AccountTypeGroup } from "./components/AccountTypeGroup";
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

const AccountTypeGroups: { [key: string]: AccountType[] } = {
    "Social": ["atproto", "mastodon", "misskey"],
    "Storage": ["onedrive", "google_drive", "dropbox"]
}

export type AccountInfo = {
    name: string,
    url: string
}

export const linkInfo: Partial<{ [key in AccountType]: AccountInfo[] | undefined }> = {
    "atproto": [{ name: "@wamwoowam.co.uk", url: "https://bsky.app/profile/wamwoowam.co.uk" }],
    "onedrive": [{ name: "wamwoowam@gmail.com", url: "https://onedrive.live.com" }],
}

export const OpenDialogContext = createContext<(service: AccountType) => void>(null)

export default function LinkedAccounts() {
    const [isOpen, setIsOpen] = useState<boolean>(false);
    const [service, setService] = useState<AccountType>()
    const openDialog = (service: AccountType) => {
        setService(service);
        setIsOpen(true);
    }

    const onClose = () => {
        setIsOpen(false);
    }

    return (
        <OpenDialogContext.Provider value={openDialog}>
            <LinkAccountDialog isShown={isOpen} onClose={onClose} service={service} />
            <div class="linked-accounts">
                {Object.entries(AccountTypeGroups)
                    .map(group => (<AccountTypeGroup key={group[0]} group={group} />))}
            </div>
        </OpenDialogContext.Provider>
    )
}