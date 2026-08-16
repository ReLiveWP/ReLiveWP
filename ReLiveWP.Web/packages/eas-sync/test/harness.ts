import { EasSession, EasTransport } from '@relivewp/eas-client';
import { MemoryStore, type EasStore } from '@relivewp/eas-store';

import { SyncEngine, type SyncEngineOptions } from '../src/index.ts';
import { AS_DEPLOYED, FakeEasServer, inboxFolder, type ServerQuirks } from './server/server.ts';

const ENDPOINT = 'https://sync.example.net/Microsoft-Server-ActiveSync';

export interface Harness {
    server: FakeEasServer;
    store: EasStore;
    session: EasSession;
    engine: SyncEngine;
    tick: () => number;
}

export function harness(options: {
    quirks?: ServerQuirks;
    engine?: SyncEngineOptions;
    seedInbox?: boolean;
    // hand one in to carry state across two engines, as a restart with new options does
    store?: EasStore;
} = {}): Harness {
    const server = new FakeEasServer(options.quirks ?? AS_DEPLOYED);
    if (options.seedInbox !== false) server.mailbox.addFolder(inboxFolder());

    const store = options.store
        ?? new MemoryStore({ userId: 'u', deviceId: 'D1', deviceType: 'Browser' });

    const transport = new EasTransport({
        endpoint: ENDPOINT,
        deviceId: 'B0045E20000000000000000000000001',
        deviceType: 'Browser',
        authorization: () => 'Basic dGVzdDp0ZXN0',
        fetch: server.fetch,
        sleep: async () => {},
    });

    const session = new EasSession(transport, {
        deviceInformation: { model: 'ReLiveWP Web', os: 'Web' },
    });

    let clock = 1_000;
    const tick = () => clock++;

    const engine = new SyncEngine(session, store, { now: tick, ...options.engine });

    return { server, store, session, engine, tick };
}

// makes the next `failures` calls to applyChanges throw, like a lost commit
export function droppingCommits(store: EasStore, failures: number): EasStore {
    let left = failures;

    return new Proxy(store, {
        get(target, property, receiver) {
            if (property !== 'applyChanges') return Reflect.get(target, property, receiver);

            return (...args: unknown[]) => {
                if (left > 0) {
                    left--;
                    return Promise.reject(new Error('commit lost'));
                }
                return (target.applyChanges as (...a: unknown[]) => unknown)(...args);
            };
        },
    });
}
