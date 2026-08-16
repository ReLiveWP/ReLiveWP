import { MINUTES_PER_DAY, addDays, minutesIntoDay, startOfDay } from '../dates.ts';
import { order } from '../order.ts';
import type { Occurrence } from '../recurrence.ts';

export const SHORT_MINUTES = 30;

export interface TimedBox {
    occurrence: Occurrence;
    top: number;
    height: number;
    left: number;
    width: number;
    short: boolean;
    abuts: boolean;
}

export interface DayLayout {
    day: number;
    boxes: TimedBox[];
    allDay: Occurrence[];
}

interface Cluster {
    width: number;
}

interface Placement {
    occurrence: Occurrence;
    start: number;
    uiEnd: number;
    short: boolean;
    cluster: Cluster;
    position: number;
    width: number;
}

function spansADay(occurrence: Occurrence): boolean {
    return addDays(occurrence.startAt, 1) <= occurrence.endAt;
}

function touches(occurrence: Occurrence, day: number, next: number): boolean {
    if (occurrence.startAt >= next) return false;

    return occurrence.endAt > day || occurrence.endAt === occurrence.startAt;
}

function place(occurrence: Occurrence, day: number, next: number): Placement {
    const start = occurrence.startAt < day ? 0 : minutesIntoDay(occurrence.startAt);
    const end = occurrence.endAt >= next ? MINUTES_PER_DAY : minutesIntoDay(occurrence.endAt);
    const short = end - start <= SHORT_MINUTES;

    return {
        occurrence,
        start,
        uiEnd: short ? start + SHORT_MINUTES : end,
        short,
        cluster: { width: 1 },
        position: 0,
        width: 1,
    };
}

function collides(a: Placement, b: Placement): boolean {
    return a.start < b.uiEnd && b.start < a.uiEnd;
}

function spread(members: Placement[], cluster: Cluster): void {
    for (const placement of members) {
        for (let lane = placement.position + 1; lane < cluster.width; lane++) {
            const blocked = members.some((other) => other.position === lane && collides(placement, other));
            if (blocked) break;

            placement.width++;
        }
    }
}

function assign(placements: Placement[]): void {
    let columns: Placement[] = [];
    let members: Placement[] = [];
    let cluster: Cluster = { width: 1 };
    let max = 0;

    for (const current of placements) {
        for (let lane = 0; lane < placements.length; lane++) {
            const occupant = columns[lane];
            if (occupant !== undefined && occupant.uiEnd > current.start) continue;

            if (lane === 0 && max <= current.start) {
                spread(members, cluster);

                cluster = { width: 1 };
                columns = [];
                members = [];
            }

            if (columns[lane] === undefined) cluster.width = Math.max(cluster.width, lane + 1);

            max = Math.max(max, current.uiEnd);

            columns[lane] = current;
            members.push(current);

            current.cluster = cluster;
            current.position = lane;
            break;
        }
    }

    spread(members, cluster);
}

export function layoutDay(occurrences: Occurrence[], at: number): DayLayout {
    const day = startOfDay(at);
    const next = addDays(day, 1);

    const allDay: Occurrence[] = [];
    const timed: Occurrence[] = [];

    for (const occurrence of occurrences) {
        if (!touches(occurrence, day, next)) continue;

        (occurrence.allDay || spansADay(occurrence) ? allDay : timed).push(occurrence);
    }

    allDay.sort(order);
    timed.sort(order);

    const placements = timed.map((occurrence) => place(occurrence, day, next));
    assign(placements);

    const borders = new Set(placements.map((placement) => placement.uiEnd));

    return {
        day,
        allDay,
        boxes: placements.map((placement) => ({
            occurrence: placement.occurrence,
            top: placement.start / MINUTES_PER_DAY,
            height: (placement.uiEnd - placement.start) / MINUTES_PER_DAY,
            left: placement.position / placement.cluster.width,
            width: placement.width / placement.cluster.width,
            short: placement.short,
            abuts: borders.has(placement.start),
        })),
    };
}
