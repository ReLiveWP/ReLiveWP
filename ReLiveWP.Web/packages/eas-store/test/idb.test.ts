import 'fake-indexeddb/auto';

import { openStore } from '../src/idb.ts';
import { runStoreSuite } from './store-suite.ts';

let next = 0;

runStoreSuite('IdbStore', () => openStore({
    userId: 'u',
    deviceId: 'D1',
    deviceType: 'Browser',
    name: `test-${next++}`,
}));
