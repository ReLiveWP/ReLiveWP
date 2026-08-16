import type { EasNode } from '@relivewp/eas-client/nodes';
import type { ItemClass } from '@relivewp/eas-store';

import { CALENDAR_SUPPORTED } from './calendar.ts';
import { CONTACT_SUPPORTED } from './contact.ts';

export { decodeEncodedWords, parseAddress, parseAddressList } from './addresses.ts';
export { collectionClassOf, folderKind, pingClassOf, readFolder } from './folders.ts';
export { readMessage, readMessageBody } from './message.ts';
export {
    readAnnotations,
    readContact,
    readContactPhoto,
    CONTACT_SUPPORTED,
    LIVE_ANNOTATIONS,
} from './contact.ts';
export { readEvent, CALENDAR_SUPPORTED, CALENDAR_REQUIRED_SUPPORTED } from './calendar.ts';
export { readTask } from './task.ts';

// only contacts and calendar can be ghosted, everything else sends whole items on a Change
export function supportedFor(itemClass: ItemClass): EasNode[] | undefined {
    if (itemClass === 'Contact') return CONTACT_SUPPORTED;
    if (itemClass === 'Calendar') return CALENDAR_SUPPORTED;
    return undefined;
}

export function supportedHash(itemClass: ItemClass): string | null {
    const supported = supportedFor(itemClass);
    if (supported === undefined) return null;

    const names = supported.map((node) => `${node.ns}:${node.name}`).sort();
    let hash = 0;
    for (const character of names.join(',')) hash = (Math.imul(hash, 31) + character.charCodeAt(0)) | 0;

    return `${names.length}-${(hash >>> 0).toString(36)}`;
}
