import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { folderSyncRecovery, pingRecovery, syncRecovery } from '../src/index.ts';

describe('Sync statuses', () => {
    it('treats success and an absent status as nothing to do', () => {
        assert.equal(syncRecovery(1).kind, 'ok');
        assert.equal(syncRecovery(undefined).kind, 'ok');
    });

    it('re-primes on an invalid sync key and re-runs FolderSync on a stale hierarchy', () => {
        assert.equal(syncRecovery(3).kind, 'reprime');
        assert.equal(syncRecovery(12).kind, 'resyncHierarchy');
    });

    it('retries the transient ones and gives up on the permanent ones', () => {
        assert.equal(syncRecovery(5).kind, 'retry');
        assert.equal(syncRecovery(16).kind, 'retry');
        assert.equal(syncRecovery(4).kind, 'fail');
        assert.equal(syncRecovery(9).kind, 'fail');
    });
});

describe('common statuses, which can arrive from any command', () => {
    it('resolves before the per-command table gets a look', () => {
        assert.equal(syncRecovery(142).kind, 'reprovision');
        assert.equal(syncRecovery(144).kind, 'reprovision');
        assert.equal(folderSyncRecovery(143).kind, 'reprovision');
        assert.equal(pingRecovery(144).kind, 'reprovision');
    });

    it('asks for a wipe on 140 and a re-prime when the server lost its own state', () => {
        assert.equal(syncRecovery(140).kind, 'wipe');
        assert.equal(syncRecovery(132).kind, 'reprime');
        assert.equal(syncRecovery(134).kind, 'reprime');
    });

    it('resends in full when the cached request could not be replayed', () => {
        assert.equal(syncRecovery(149).kind, 'retry');
    });
});

describe('FolderSync statuses', () => {
    it('rewinds the hierarchy on a key mismatch', () => {
        assert.equal(folderSyncRecovery(9).kind, 'reprime');
        assert.equal(folderSyncRecovery(6).kind, 'retry');
        assert.equal(folderSyncRecovery(10).kind, 'fail');
    });
});

describe('Ping statuses', () => {
    it('accepts both 1 and 2 as ordinary outcomes', () => {
        assert.equal(pingRecovery(1).kind, 'ok');
        assert.equal(pingRecovery(2).kind, 'ok');
    });

    it('retries the negotiable ones and re-runs FolderSync when told to', () => {
        assert.equal(pingRecovery(3).kind, 'retry');
        assert.equal(pingRecovery(5).kind, 'retry');
        assert.equal(pingRecovery(6).kind, 'retry');
        assert.equal(pingRecovery(7).kind, 'resyncHierarchy');
        assert.equal(pingRecovery(4).kind, 'fail');
    });
});
