import "./folder-list.scss";

import type { Folder, FolderCounts } from "@relivewp/eas-store";

type Props = {
    folders: Folder[],
    counts: Record<string, FolderCounts>,
    selected: string | null,
    onSelect: (id: string) => void,
};

export default function FolderList({ folders, counts, selected, onSelect }: Props) {
    return (
        <nav class="folder-list">
            <ul>
                {folders.map((folder) => {
                    const unread = counts[folder.id]?.unread ?? 0;
                    const current = folder.id === selected;

                    return (
                        <li key={folder.id}>
                            <button
                                type="button"
                                class={current ? "text-button icon on" : "text-button icon"}
                                aria-current={current ? "true" : undefined}
                                onClick={() => { onSelect(folder.id); }}
                            >
                                <span class="folder-name">{folder.name}</span>
                                {unread > 0 && <span class="folder-unread">{unread}</span>}
                            </button>
                        </li>
                    );
                })}
            </ul>
        </nav>
    );
}
