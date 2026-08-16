import { MAILBOX, type EasSession, type FetchResponse } from '@relivewp/eas-client';
import type { EasStore, Message } from '@relivewp/eas-store';

import { readMessageBody } from '../project/index.ts';
import { itemOperationsRecovery, type Recovery } from './status.ts';

export interface FetchBodyOptions {
    signal?: AbortSignal | undefined;
}

export interface FetchBodyOutcome {
    recovery: Recovery;
    status: number | undefined;
    message: Message | undefined;
    fetched: boolean;
}

function outcome(patch: Partial<FetchBodyOutcome>): FetchBodyOutcome {
    return {
        recovery: { kind: 'ok' },
        status: undefined,
        message: undefined,
        fetched: false,
        ...patch,
    };
}

export async function fetchMessageBody(
    session: EasSession,
    store: EasStore,
    folderId: string,
    messageId: string,
    options: FetchBodyOptions = {}): Promise<FetchBodyOutcome> {
    const stored = await store.message(folderId, messageId);

    if (stored === undefined)
        return outcome({ recovery: { kind: 'fail', reason: 'no such message' } });

    if (stored.body === null || !stored.body.truncated)
        return outcome({ message: stored });

    const post = options.signal === undefined ? {} : { signal: options.signal };
    const { parsed } = await session.itemOperations({
        operations: [{
            kind: 'Fetch',
            store: MAILBOX,
            collectionId: folderId,
            serverId: messageId,
            options: { bodyPreference: [{ type: stored.body.type === 'html' ? 2 : 1 }] },
        }],
    }, post);

    if (parsed === undefined)
        return outcome({
            recovery: { kind: 'fail', reason: 'the server returned no body' },
            message: stored,
        });

    const operation = parsed.operations.find(
        (entry): entry is FetchResponse => entry.kind === 'Fetch');

    const status = operation?.status ?? parsed.status;
    const recovery = itemOperationsRecovery(status);
    if (recovery.kind !== 'ok') return outcome({ recovery, status, message: stored });

    const body = readMessageBody(operation?.properties);

    if (body === null)
        return outcome({
            recovery: { kind: 'fail', reason: 'the fetch carried no body' },
            status,
            message: stored,
        });

    const patched = await store.patchMessage(folderId, messageId, { body });
    return outcome({ status, message: patched ?? stored, fetched: true });
}
