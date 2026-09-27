import { int, tags, type EasNode } from '@relivewp/eas-client/nodes';
import type { CursorOptions, ItemClass, PolicySettings } from '@relivewp/eas-store';

const { Provision: P } = tags;

export const DEFAULT_POLICY: PolicySettings = {
    allowHtmlEmail: true,
    maxEmailBodyTruncationSize: null,
    maxEmailHtmlBodyTruncationSize: null,
    maxAttachmentSize: null,
    maxEmailAgeFilter: null,
    maxCalendarAgeFilter: null,
};

export const DEFAULT_TRUNCATION = 32768;
export const DEFAULT_WINDOW_SIZE = 100;

function optional(doc: EasNode | undefined, tag: typeof P.MaxAttachmentSize): number | null {
    return int(doc, tag) ?? null;
}

export function readPolicy(doc: EasNode | undefined): PolicySettings {
    if (doc === undefined) return DEFAULT_POLICY;

    return {
        allowHtmlEmail: int(doc, P.AllowHTMLEmail) !== 0,
        maxEmailBodyTruncationSize: optional(doc, P.MaxEmailBodyTruncationSize),
        maxEmailHtmlBodyTruncationSize: optional(doc, P.MaxEmailHTMLBodyTruncationSize),
        maxAttachmentSize: optional(doc, P.MaxAttachmentSize),
        maxEmailAgeFilter: optional(doc, P.MaxEmailAgeFilter),
        maxCalendarAgeFilter: optional(doc, P.MaxCalendarAgeFilter),
    };
}

// -1 is no limit, 0 is headers only. 0 is a real value, not an absence.
function truncationFor(limit: number | null): number | null {
    if (limit === null) return DEFAULT_TRUNCATION;
    if (limit < 0) return null;
    return limit;
}

export function annotationsFor(
    itemClass: ItemClass, requested: readonly string[] | undefined): string[] | null {
    if (itemClass !== 'Contact' || requested === undefined || requested.length === 0) return null;
    return [...requested].sort();
}

export function cursorOptionsFor(
    policy: PolicySettings,
    itemClass: ItemClass,
    windowSize: number = DEFAULT_WINDOW_SIZE,
    annotations: readonly string[] | undefined = undefined): CursorOptions {
    const html = policy.allowHtmlEmail;
    const limit = html ? policy.maxEmailHtmlBodyTruncationSize : policy.maxEmailBodyTruncationSize;

    const ageFilter = itemClass === 'Calendar'
        ? policy.maxCalendarAgeFilter
        : itemClass === 'Email' ? policy.maxEmailAgeFilter : null;

    return {
        windowSize,
        bodyType: html ? 'html' : 'text',
        truncationSize: truncationFor(limit),
        // a filter on contacts has to be ignored, not just may be
        filterType: itemClass === 'Contact' || itemClass === 'Note' ? null : ageFilter,
        conversationMode: false,
        annotations: annotationsFor(itemClass, annotations),
    };
}

function annotationKey(options: CursorOptions): string {
    return (options.annotations ?? []).join('\u0000');
}

export function sameOptions(left: CursorOptions, right: CursorOptions): boolean {
    return left.windowSize === right.windowSize
        && left.bodyType === right.bodyType
        && left.truncationSize === right.truncationSize
        && left.filterType === right.filterType
        && left.conversationMode === right.conversationMode
        && annotationKey(left) === annotationKey(right);
}
