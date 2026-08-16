import type { EasSession } from '@relivewp/eas-client';
import type { EasStore, Folder } from '@relivewp/eas-store';

import { readFolder } from '../project/folders.ts';
import { folderSyncRecovery, type Recovery } from './status.ts';

export interface HierarchyOutcome {
    recovery: Recovery;
    status: number | undefined;
    added: number;
    removed: number;
    unchanged: boolean;
}

export interface HierarchyOptions {
    now: () => number;
    signal?: AbortSignal | undefined;
}

export async function syncHierarchy(
    session: EasSession, store: EasStore, options: HierarchyOptions): Promise<HierarchyOutcome> {
    const account = await store.account();
    const { parsed } = await session.folderSync(
        account.folderSyncKey,
        options.signal === undefined ? {} : { signal: options.signal });

    if (parsed === undefined)
        return { recovery: { kind: 'ok' }, status: undefined, added: 0, removed: 0, unchanged: true };

    const recovery = folderSyncRecovery(parsed.status);
    if (recovery.kind !== 'ok')
        return { recovery, status: parsed.status, added: 0, removed: 0, unchanged: false };

    const toSyncKey = parsed.syncKey;
    if (toSyncKey === undefined)
        return {
            recovery: { kind: 'fail', reason: 'the server returned no folder sync key' },
            status: parsed.status,
            added: 0,
            removed: 0,
            unchanged: false,
        };

    const upsert: Folder[] = [];
    const remove: string[] = [];

    for (const change of parsed.changes) {
        if (change.kind === 'Delete') {
            if (change.serverId !== undefined) remove.push(change.serverId);
            continue;
        }

        const folder = readFolder(change);
        if (folder !== undefined) upsert.push(folder);
    }

    const applied = await store.applyFolderChanges({
        fromSyncKey: account.folderSyncKey,
        toSyncKey,
        syncedAt: options.now(),
        upsert,
        remove,
    });

    if (!applied.applied)
        return {
            recovery: { kind: 'retry' },
            status: parsed.status,
            added: 0,
            removed: 0,
            unchanged: false,
        };

    return {
        recovery: { kind: 'ok' },
        status: parsed.status,
        added: upsert.length,
        removed: remove.length,
        unchanged: upsert.length === 0 && remove.length === 0,
    };
}

export async function resetHierarchy(store: EasStore): Promise<void> {
    const folders = await store.folders();
    const account = await store.account();

    await store.applyFolderChanges({
        fromSyncKey: account.folderSyncKey,
        toSyncKey: '0',
        syncedAt: 0,
        upsert: [],
        remove: folders.map((folder) => folder.id),
    });
}
