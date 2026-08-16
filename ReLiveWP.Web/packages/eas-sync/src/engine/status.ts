export type Recovery =
    | { kind: 'ok' }
    | { kind: 'retry' }
    | { kind: 'reprime' }
    | { kind: 'resyncHierarchy' }
    | { kind: 'reprovision' }
    | { kind: 'wipe' }
    | { kind: 'fail'; reason: string };

const OK: Recovery = { kind: 'ok' };
const RETRY: Recovery = { kind: 'retry' };
const REPRIME: Recovery = { kind: 'reprime' };
const HIERARCHY: Recovery = { kind: 'resyncHierarchy' };
const REPROVISION: Recovery = { kind: 'reprovision' };

function fail(reason: string): Recovery {
    return { kind: 'fail', reason };
}

// 100 and up can come back from any command, so check those first
function common(status: number): Recovery | undefined {
    if (status < 100) return undefined;

    switch (status) {
        case 110:
            return fail('server error, do not retry');
        case 111:
        case 133:
            return RETRY;
        case 132:
        case 134:
            return REPRIME;
        case 140:
            return { kind: 'wipe' };
        case 141:
        case 142:
        case 143:
        case 144:
            return REPROVISION;
        case 145:
            return fail('server rejects externally managed devices');
        case 149:
            return RETRY;
        case 177:
            return fail('too many device partnerships');
        default:
            return fail(`status ${status}`);
    }
}

export function syncRecovery(status: number | undefined): Recovery {
    if (status === undefined || status === 1) return OK;

    const shared = common(status);
    if (shared !== undefined) return shared;

    switch (status) {
        case 3:
            return REPRIME;
        case 5:
        case 16:
            return RETRY;
        case 12:
            return HIERARCHY;
        case 13:
            return RETRY;
        case 4:
            return fail('the server rejected the request as malformed');
        case 8:
            return fail('the collection or item no longer exists');
        case 9:
            return fail('the mailbox is out of space');
        case 14:
            return fail('the heartbeat or wait value was out of range');
        case 15:
            return fail('too many collections in one request');
        default:
            return fail(`sync status ${status}`);
    }
}

export function folderSyncRecovery(status: number | undefined): Recovery {
    if (status === undefined || status === 1) return OK;

    const shared = common(status);
    if (shared !== undefined) return shared;

    switch (status) {
        case 6:
        case 11:
            return RETRY;
        case 9:
            return { kind: 'reprime' };
        case 10:
            return fail('the server rejected the folder request as malformed');
        default:
            return fail(`foldersync status ${status}`);
    }
}

export function itemOperationsRecovery(status: number | undefined): Recovery {
    if (status === undefined || status === 1 || status === 17) return OK;

    const shared = common(status);
    if (shared !== undefined) return shared;

    switch (status) {
        case 3:
        case 12:
            return RETRY;
        case 2:
            return fail('the server rejected the request as malformed');
        case 4:
        case 5:
        case 6:
        case 7:
            return fail('the document library refused the request');
        case 8:
            return fail('the byte range is invalid or too large');
        case 9:
            return fail('the store is unknown or unsupported');
        case 10:
            return fail('the item is empty');
        case 11:
            return fail('the requested data size is too large');
        case 14:
            return fail('the item failed conversion');
        case 15:
            return fail('the attachment or its identifier is invalid');
        case 16:
            return fail('access to the item is denied');
        case 18:
            return fail('the server wants credentials');
        default:
            return fail(`itemoperations status ${status}`);
    }
}

export function pingRecovery(status: number | undefined): Recovery {
    if (status === undefined || status === 1 || status === 2) return OK;

    const shared = common(status);
    if (shared !== undefined) return shared;

    switch (status) {
        case 3:
        case 5:
        case 6:
            return { kind: 'retry' };
        case 7:
            return HIERARCHY;
        case 8:
            return RETRY;
        case 4:
            return fail('the server rejected the ping as malformed');
        default:
            return fail(`ping status ${status}`);
    }
}
