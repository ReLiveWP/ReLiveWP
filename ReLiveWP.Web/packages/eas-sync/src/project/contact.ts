import { CONTACT_PROPERTIES, readContact as readContactItem, type ContactItem } from '@relivewp/eas-client';
import {
    elements, isOpaque, isText, pick, tags, textOf, type EasNode,
} from '@relivewp/eas-client/nodes';
import type { Contact, ContactAnnotation, LabelledValue, PostalAddress } from '@relivewp/eas-store';

import { decodeEncodedWords } from './addresses.ts';
import {
    decodeBase64, epoch, labelled, readBody, readCategories, supportedFrom,
} from './shared.ts';

const { Contacts, WindowsLive: WL } = tags;

export const CONTACT_SUPPORTED: EasNode[] =
    supportedFrom(CONTACT_PROPERTIES, ['Contacts', 'Contacts2']);

export const LIVE_ANNOTATIONS: readonly string[] = [
    'CID', 'OID', 'WLID', 'IMMRI', 'Type', 'UserTileUrl', 'UserTileHash', 'TrustLevel',
    'FavoriteOrder',
];

const EMPTY_ANNOTATION: ContactAnnotation = {
    cid: null, objectId: null, wlid: null, imMri: null, type: null,
    userTileUrl: null, userTileHash: null, trustLevel: null, favouriteOrder: null,
};

function annotationInt(value: string | null): number | null {
    if (value === null) return null;

    const parsed = Number(value);
    return Number.isInteger(parsed) ? parsed : null;
}

export function readAnnotations(data: EasNode): ContactAnnotation | null {
    const container = pick(data, WL.Annotations);
    if (container === undefined) return null;

    const values = new Map<string, string>();
    for (const entry of elements(container)) {
        if (entry.ns !== WL.Annotation.ns || entry.name !== WL.Annotation.name) continue;

        const name = pick(entry, WL.Name);
        const value = pick(entry, WL.Value);
        if (name === undefined) continue;

        const text = value === undefined ? '' : textOf(value);
        if (text.length > 0) values.set(textOf(name), text);
    }

    const read = (name: string): string | null => values.get(name) ?? null;

    return {
        ...EMPTY_ANNOTATION,
        cid: read('CID'),
        objectId: read('OID'),
        wlid: read('WLID'),
        imMri: read('IMMRI'),
        type: read('Type'),
        userTileUrl: read('UserTileUrl'),
        userTileHash: read('UserTileHash'),
        trustLevel: annotationInt(read('TrustLevel')),
        favouriteOrder: annotationInt(read('FavoriteOrder')),
    };
}

export function readContactPhoto(data: EasNode): Uint8Array | undefined {
    const node = pick(data, Contacts.Picture);
    if (node === undefined) return undefined;

    for (const child of node.children) if (isOpaque(child)) return child.opaque;

    let encoded = '';
    for (const child of node.children) if (isText(child)) encoded += child.text;

    return encoded.length === 0 ? new Uint8Array() : decodeBase64(encoded);
}

function address(label: string, parts: {
    street?: string | undefined;
    city?: string | undefined;
    state?: string | undefined;
    postalCode?: string | undefined;
    country?: string | undefined;
}): PostalAddress | undefined {
    const filled = Object.values(parts).some((part) => part !== undefined && part.length > 0);
    if (!filled) return undefined;

    return {
        label,
        street: parts.street ?? null,
        city: parts.city ?? null,
        state: parts.state ?? null,
        postalCode: parts.postalCode ?? null,
        country: parts.country ?? null,
    };
}

function displayNameOf(item: ContactItem): string {
    const full = [item.firstName, item.middleName, item.lastName]
        .filter((part): part is string => part !== undefined && part.length > 0)
        .join(' ');

    return decodeEncodedWords(
        item.fileAs ?? (full.length > 0 ? full : undefined)
        ?? item.companyName
        ?? item.email1Address
        ?? '');
}

function sortNameOf(item: ContactItem, displayName: string): string {
    const parts = [item.lastName, item.firstName]
        .filter((part): part is string => part !== undefined && part.length > 0);

    return (parts.length > 0 ? parts.join(' ') : displayName).toLowerCase();
}

function phones(item: ContactItem): LabelledValue[] {
    return labelled([
        ['mobile', item.mobilePhoneNumber],
        ['home', item.homePhoneNumber],
        ['home2', item.home2PhoneNumber],
        ['work', item.businessPhoneNumber],
        ['work2', item.business2PhoneNumber],
        ['company', item.companyMainPhone],
        ['assistant', item.assistantPhoneNumber],
        ['car', item.carPhoneNumber],
        ['radio', item.radioPhoneNumber],
        ['pager', item.pagerNumber],
        ['homeFax', item.homeFaxNumber],
        ['workFax', item.businessFaxNumber],
        ['mms', item.mms],
    ]);
}

export function readContact(data: EasNode, folderId: string, id: string): Contact {
    const item = readContactItem(data);
    const displayName = displayNameOf(item);

    const addresses = [
        address('home', {
            street: item.homeAddressStreet, city: item.homeAddressCity,
            state: item.homeAddressState, postalCode: item.homeAddressPostalCode,
            country: item.homeAddressCountry,
        }),
        address('work', {
            street: item.businessAddressStreet, city: item.businessAddressCity,
            state: item.businessAddressState, postalCode: item.businessAddressPostalCode,
            country: item.businessAddressCountry,
        }),
        address('other', {
            street: item.otherAddressStreet, city: item.otherAddressCity,
            state: item.otherAddressState, postalCode: item.otherAddressPostalCode,
            country: item.otherAddressCountry,
        }),
    ].filter((entry): entry is PostalAddress => entry !== undefined);

    return {
        id,
        folderId,
        displayName,
        sortName: sortNameOf(item, displayName),
        firstName: item.firstName ?? null,
        middleName: item.middleName ?? null,
        lastName: item.lastName ?? null,
        nickname: item.nickName ?? null,
        company: item.companyName ?? null,
        jobTitle: item.jobTitle ?? null,
        department: item.department ?? null,
        officeLocation: item.officeLocation ?? null,
        emails: labelled([
            ['email1', item.email1Address],
            ['email2', item.email2Address],
            ['email3', item.email3Address],
        ]),
        phones: phones(item),
        imAddresses: labelled([
            ['im', item.imAddress],
            ['im2', item.imAddress2],
            ['im3', item.imAddress3],
        ]),
        addresses,
        webPage: item.webPage ?? null,
        birthday: epoch(item.birthday),
        anniversary: epoch(item.anniversary),
        categories: readCategories(item.categories),
        notes: readBody(item.body),
        annotation: readAnnotations(data),
    };
}
