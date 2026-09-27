import type { EasSession, SyncCollectionRequest } from '@relivewp/eas-client';
import type { ChangeSet, Cursor, EasStore, Item, ItemClass } from '@relivewp/eas-store';

import type { EasNode } from '@relivewp/eas-client/nodes';

import {
    collectionClassOf,
    readContact,
    readContactPhoto,
    readEvent,
    readMessage,
    readTask,
    supportedFor,
} from '../project/index.ts';
import { syncRecovery, type Recovery } from './status.ts';

export const BARREN_ROUND_LIMIT = 3;

export interface CollectionOutcome {
    folderId: string;
    recovery: Recovery;
    status: number | undefined;
    rounds: number;
    delivered: number;
    removed: number;
    converged: boolean;
    unchanged: boolean;
}

export interface CollectionOptions {
    now: () => number;
    maxRounds?: number | undefined;
    signal?: AbortSignal | undefined;
    onProgress?: ((folderId: string) => void) | undefined;
}

export function buildRequest(cursor: Cursor): SyncCollectionRequest {
    const { options } = cursor;
    // only read while priming, and what it names is what the server stops ghosting, for good
    const supported = cursor.primed ? undefined : supportedFor(cursor.class);

    return {
        collectionId: cursor.folderId,
        syncKey: cursor.syncKey,
        ...(supported === undefined ? {} : { supported }),
        // without this the server doesn't have to report MoreAvailable at all
        windowSize: options.windowSize,
        ...(cursor.primed ? { getChanges: true } : {}),
        ...(options.conversationMode ? { conversationMode: true } : {}),
        options: [{
            class: collectionClassOf(cursor.class),
            ...(options.filterType === null ? {} : { filterType: options.filterType }),
            bodyPreference: [{
                type: options.bodyType === 'html' ? 2 : 1,
                ...(options.truncationSize === null ? {} : { truncationSize: options.truncationSize }),
                preview: 255,
            }],
            // sent on every request, not just the prime that reads it: a re-init that omitted it
            // would leave the server caching an empty set and annotations would stop arriving
            ...(options.annotations === null ? {} : { annotations: options.annotations }),
        }],
    };
}

interface Incoming {
    serverId: string;
    applicationData: EasNode;
}

interface Keys {
    fromSyncKey: string;
    toSyncKey: string;
    syncedAt: number;
    moreAvailable: boolean;
}

function changeSet(
    itemClass: ItemClass,
    folderId: string,
    keys: Keys,
    incoming: Incoming[],
    remove: string[]): ChangeSet {
    const shared = { folderId, ...keys, remove };
    const project = <T>(read: (data: EasNode, folderId: string, id: string) => T): T[] =>
        incoming.map((entry) => read(entry.applicationData, folderId, entry.serverId));

    switch (itemClass) {
        case 'Contact':
            return {
                class: 'Contact',
                ...shared,
                upsert: project(readContact),
                // only the ones that carried a Picture at all: an omitted one means unchanged
                photos: incoming.flatMap((entry) => {
                    const bytes = readContactPhoto(entry.applicationData);
                    return bytes === undefined ? [] : [{ id: entry.serverId, bytes }];
                }),
            };
        case 'Calendar':
            return { class: 'Calendar', ...shared, upsert: project(readEvent) };
        case 'Task':
            return { class: 'Task', ...shared, upsert: project(readTask) };
        default:
            return { class: 'Email', ...shared, upsert: project(readMessage) };
    }
}

function outcome(folderId: string, patch: Partial<CollectionOutcome>): CollectionOutcome {
    return {
        folderId,
        recovery: { kind: 'ok' },
        status: undefined,
        rounds: 0,
        delivered: 0,
        removed: 0,
        converged: true,
        unchanged: false,
        ...patch,
    };
}

export async function syncCollection(
    session: EasSession,
    store: EasStore,
    folderId: string,
    options: CollectionOptions): Promise<CollectionOutcome> {
    const limit = options.maxRounds ?? 200;
    const post = options.signal === undefined ? {} : { signal: options.signal };

    let rounds = 0;
    let delivered = 0;
    let removed = 0;
    let barren = 0;

    for (;;) {
        const cursor = await store.cursor(folderId);
        if (cursor === undefined)
            return outcome(folderId, {
                recovery: { kind: 'fail', reason: 'no cursor for this folder' },
                rounds,
                converged: false,
            });

        if (rounds >= limit)
            return outcome(folderId, {
                recovery: { kind: 'fail', reason: `gave up after ${rounds} rounds` },
                rounds,
                delivered,
                removed,
                converged: false,
            });

        const priming = !cursor.primed;
        const { parsed } = await session.sync(
            { collections: [buildRequest(cursor)] }, post);
        rounds++;

        // no body means nothing changed
        if (parsed === undefined)
            return outcome(folderId, {
                rounds,
                delivered,
                removed,
                unchanged: delivered === 0 && removed === 0,
            });

        const collection = parsed.collections[0];
        const status = collection?.status ?? parsed.status;
        const recovery = syncRecovery(status);

        if (recovery.kind !== 'ok')
            return outcome(folderId, { recovery, status, rounds, delivered, removed, converged: false });

        if (collection === undefined || collection.syncKey === undefined)
            return outcome(folderId, {
                recovery: { kind: 'fail', reason: 'the server returned no sync key' },
                status,
                rounds,
                delivered,
                removed,
                converged: false,
            });

        const incoming: Incoming[] = [];
        const remove: string[] = [];

        for (const change of collection.changes) {
            if (change.kind === 'Add' || change.kind === 'Change') {
                if (change.applicationData !== undefined)
                    incoming.push({
                        serverId: change.serverId,
                        applicationData: change.applicationData,
                    });
                continue;
            }

            remove.push(change.serverId);
        }

        const applied = await store.applyChanges(changeSet(cursor.class, folderId, {
            fromSyncKey: cursor.syncKey,
            toSyncKey: collection.syncKey,
            syncedAt: options.now(),
            moreAvailable: collection.moreAvailable,
        }, incoming, remove));

        if (!applied.applied)
            return outcome(folderId, {
                recovery: { kind: 'retry' },
                status,
                rounds,
                delivered,
                removed,
                converged: false,
            });

        delivered += incoming.length;
        removed += remove.length;

        // the round is committed, so say so now rather than at the end of the drain
        if (incoming.length > 0 || remove.length > 0) options.onProgress?.(folderId);

        // a priming response has no items and no MoreAvailable to say more are coming
        if (priming) continue;

        if (!collection.moreAvailable)
            return outcome(folderId, {
                status,
                rounds,
                delivered,
                removed,
                unchanged: delivered === 0 && removed === 0,
            });

        // server says there's more but keeps sending none, we'd loop forever
        barren = incoming.length === 0 && remove.length === 0 ? barren + 1 : 0;
        if (barren >= BARREN_ROUND_LIMIT)
            return outcome(folderId, {
                recovery: {
                    kind: 'fail',
                    reason: `the server reported more changes but delivered none for ${barren} rounds`,
                },
                status,
                rounds,
                delivered,
                removed,
                converged: false,
            });
    }
}
