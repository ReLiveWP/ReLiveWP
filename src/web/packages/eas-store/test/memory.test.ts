import { MemoryStore } from '../src/index.ts';
import { runStoreSuite } from './store-suite.ts';

runStoreSuite('MemoryStore', () =>
    Promise.resolve(new MemoryStore({ userId: 'u', deviceId: 'D1', deviceType: 'Browser' })));
