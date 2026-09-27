export {
    collectionClassOf,
    decodeEncodedWords,
    folderKind,
    parseAddress,
    parseAddressList,
    pingClassOf,
    readContact,
    readEvent,
    readFolder,
    readMessage,
    readMessageBody,
    readTask,
    supportedFor,
    supportedHash,
    CALENDAR_REQUIRED_SUPPORTED,
    CALENDAR_SUPPORTED,
    CONTACT_SUPPORTED,
    LIVE_ANNOTATIONS,
    readAnnotations,
    readContactPhoto,
} from './project/index.ts';

export { ensureProvisioned, type AccountOptions, type ProvisionOutcome } from './engine/account.ts';
export {
    buildRequest,
    syncCollection,
    BARREN_ROUND_LIMIT,
    type CollectionOptions,
    type CollectionOutcome,
} from './engine/collection.ts';
export {
    SyncEngine,
    type SyncEngineOptions,
    type SyncProgress,
    type SyncReport,
} from './engine/engine.ts';
export {
    fetchMessageBody,
    type FetchBodyOptions,
    type FetchBodyOutcome,
} from './engine/fetch.ts';
export {
    resetHierarchy,
    syncHierarchy,
    type HierarchyOptions,
    type HierarchyOutcome,
} from './engine/hierarchy.ts';
export {
    cursorOptionsFor,
    readPolicy,
    sameOptions,
    DEFAULT_POLICY,
    DEFAULT_TRUNCATION,
    DEFAULT_WINDOW_SIZE,
} from './engine/policy.ts';
export {
    folderSyncRecovery,
    itemOperationsRecovery,
    pingRecovery,
    syncRecovery,
    type Recovery,
} from './engine/status.ts';
export {
    Scheduler,
    BACKOFF_SECONDS,
    DEFAULT_HEARTBEAT_SECONDS,
    DEFAULT_MAX_FOLDERS,
    MAX_HEARTBEAT_SECONDS,
    MIN_CYCLE_MS,
    MIN_HEARTBEAT_SECONDS,
    type SchedulerOptions,
    type SchedulerPhase,
    type SchedulerStatus,
    type SyncTarget,
} from './schedule/scheduler.ts';
