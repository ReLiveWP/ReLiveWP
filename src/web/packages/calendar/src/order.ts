import type { BusyStatus } from '@relivewp/eas-store';

import type { Occurrence } from './recurrence.ts';

const RANK: Readonly<Record<BusyStatus, number>> = {
    free: 0, tentative: 1, busy: 2, outOfOffice: 3, workingElsewhere: 4,
};

export function order(a: Occurrence, b: Occurrence): number {
    if (a.startAt !== b.startAt) return a.startAt - b.startAt;
    if (a.busy !== b.busy) return RANK[b.busy] - RANK[a.busy];
    if (a.endAt !== b.endAt) return b.endAt - a.endAt;

    return a.key < b.key ? -1 : a.key > b.key ? 1 : 0;
}
