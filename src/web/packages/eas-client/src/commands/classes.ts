export const COLLECTION_CLASSES = ['Email', 'Calendar', 'Contacts', 'Tasks', 'Notes', 'SMS'] as const;

export type CollectionClass = (typeof COLLECTION_CLASSES)[number];

// ping doesn't take the SMS class, sync does
export const PING_CLASSES = ['Email', 'Calendar', 'Contacts', 'Tasks', 'Notes'] as const;

export type PingClass = (typeof PING_CLASSES)[number];
