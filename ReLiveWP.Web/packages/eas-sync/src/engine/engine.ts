import type { EasSession } from '@relivewp/eas-client';
import type { Cursor, EasStore, Folder, ItemClass } from '@relivewp/eas-store';

import { supportedHash } from '../project/index.ts';
import { ensureProvisioned, type ProvisionOutcome } from './account.ts';
import { syncCollection, type CollectionOutcome } from './collection.ts';
import { fetchMessageBody, type FetchBodyOutcome } from './fetch.ts';
import { resetHierarchy, syncHierarchy, type HierarchyOutcome } from './hierarchy.ts';
import { cursorOptionsFor, sameOptions, DEFAULT_POLICY } from './policy.ts';
import type { Recovery } from './status.ts';

const SYNCED_CLASSES: readonly ItemClass[] = ['Email', 'Contact', 'Calendar', 'Task'];

export type SyncProgress = { kind: 'folders' } | { kind: 'items'; folderId: string };

export interface SyncEngineOptions {
    now?: (() => number) | undefined;
    maxRounds?: number | undefined;
    windowSize?: number | undefined;
    signal?: AbortSignal | undefined;
    onProgress?: ((progress: SyncProgress) => void) | undefined;
    annotations?: readonly string[] | undefined;
}

export interface SyncReport {
    recovery: Recovery;
    provision: ProvisionOutcome | undefined;
    hierarchy: HierarchyOutcome | undefined;
    folders: CollectionOutcome[];
}

export class SyncEngine {
    private readonly session: EasSession;
    private readonly store: EasStore;
    private readonly now: () => number;
    private readonly options: SyncEngineOptions;

    constructor(session: EasSession, store: EasStore, options: SyncEngineOptions = {}) {
        this.session = session;
        this.store = store;
        this.now = options.now ?? (() => Date.now());
        this.options = options;
    }

    async synchronise(): Promise<SyncReport> {
        const provision = await ensureProvisioned(this.session, this.store, { now: this.now });
        if (provision.recovery.kind !== 'ok')
            return { recovery: provision.recovery, provision, hierarchy: undefined, folders: [] };

        const hierarchy = await this.hierarchy();
        if (hierarchy.recovery.kind !== 'ok')
            return { recovery: hierarchy.recovery, provision, hierarchy, folders: [] };

        const folders = await this.syncAll();
        return { recovery: { kind: 'ok' }, provision, hierarchy, folders };
    }

    async syncAll(): Promise<CollectionOutcome[]> {
        const syncable = await this.syncableFolders();
        const outcomes: CollectionOutcome[] = [];

        // serial on purpose, two syncs at once on one account gets the sync state refused
        for (const folder of syncable) outcomes.push(await this.syncFolder(folder.id));

        return outcomes;
    }

    async applyPing(folderIds: readonly string[]): Promise<CollectionOutcome[]> {
        const wanted = new Set(folderIds);
        const syncable = await this.syncableFolders();
        const outcomes: CollectionOutcome[] = [];

        for (const folder of syncable)
            if (wanted.has(folder.id)) outcomes.push(await this.syncFolder(folder.id));

        return outcomes;
    }

    async syncFolder(folderId: string): Promise<CollectionOutcome> {
        const folders = await this.store.folders();
        const folder = folders.find((candidate) => candidate.id === folderId);

        if (folder === undefined || folder.class === null)
            return {
                folderId,
                recovery: { kind: 'fail', reason: 'not a synchronised folder' },
                status: undefined,
                rounds: 0,
                delivered: 0,
                removed: 0,
                converged: false,
                unchanged: true,
            };

        await this.ensureCursor(folder, folder.class);
        const first = await this.run(folderId);
        if (first.recovery.kind === 'ok') return first;

        const recovered = await this.recover(first.recovery, folderId);
        if (!recovered) return first;

        return this.run(folderId);
    }

    async fetchBody(folderId: string, messageId: string): Promise<FetchBodyOutcome> {
        const first = await this.fetch(folderId, messageId);
        if (first.recovery.kind === 'ok') return first;

        const recovered = await this.recover(first.recovery);
        if (!recovered) return first;

        return this.fetch(folderId, messageId);
    }

    private fetch(folderId: string, messageId: string): Promise<FetchBodyOutcome> {
        return fetchMessageBody(this.session, this.store, folderId, messageId,
            this.options.signal === undefined ? {} : { signal: this.options.signal });
    }

    private async run(folderId: string): Promise<CollectionOutcome> {
        const onProgress = this.options.onProgress;

        return syncCollection(this.session, this.store, folderId, {
            now: this.now,
            ...(this.options.maxRounds === undefined ? {} : { maxRounds: this.options.maxRounds }),
            ...(this.options.signal === undefined ? {} : { signal: this.options.signal }),
            ...(onProgress === undefined
                ? {}
                : { onProgress: (id: string) => { onProgress({ kind: 'items', folderId: id }); } }),
        });
    }

    private async recover(recovery: Recovery, folderId?: string): Promise<boolean> {
        switch (recovery.kind) {
            case 'reprime':
                if (folderId === undefined) return false;
                await this.store.resetFolder(folderId);
                return true;
            case 'reprovision': {
                const outcome = await ensureProvisioned(
                    this.session, this.store, { now: this.now, force: true });
                return outcome.recovery.kind === 'ok';
            }
            // provisioning again is what surfaces the wipe directive, nothing to retry after it
            case 'wipe':
                await ensureProvisioned(this.session, this.store, { now: this.now, force: true });
                return false;
            case 'resyncHierarchy': {
                const outcome = await this.hierarchy();
                return outcome.recovery.kind === 'ok';
            }
            case 'retry':
                return true;
            default:
                return false;
        }
    }

    private async hierarchy(): Promise<HierarchyOutcome> {
        const outcome = await this.runHierarchy();

        // folders first, so a list can be drawn and a folder picked before any items land
        if (outcome.recovery.kind === 'ok' && !outcome.unchanged)
            this.options.onProgress?.({ kind: 'folders' });

        return outcome;
    }

    private async runHierarchy(): Promise<HierarchyOutcome> {
        const first = await syncHierarchy(this.session, this.store, {
            now: this.now,
            ...(this.options.signal === undefined ? {} : { signal: this.options.signal }),
        });

        if (first.recovery.kind !== 'reprime') return first;

        await resetHierarchy(this.store);
        return syncHierarchy(this.session, this.store, {
            now: this.now,
            ...(this.options.signal === undefined ? {} : { signal: this.options.signal }),
        });
    }

    private async syncableFolders(): Promise<Folder[]> {
        const folders = await this.store.folders();
        return folders.filter(
            (folder) => folder.class !== null && SYNCED_CLASSES.includes(folder.class));
    }

    private async ensureCursor(folder: Folder, itemClass: ItemClass): Promise<Cursor> {
        const account = await this.store.account();
        const wanted = cursorOptionsFor(
            account.policy ?? DEFAULT_POLICY, itemClass, this.options.windowSize,
            this.options.annotations);
        const init = {
            folderId: folder.id,
            class: itemClass,
            options: wanted,
            supportedHash: supportedHash(itemClass),
        };

        const existing = await this.store.ensureCursor(init);
        if (sameOptions(existing.options, wanted) && existing.supportedHash === init.supportedHash)
            return existing;

        // options stick server-side and Supported is only read while priming, so either change
        // means priming again
        return this.store.replaceCursor(init);
    }
}
