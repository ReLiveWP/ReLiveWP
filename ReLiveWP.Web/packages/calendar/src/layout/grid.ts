import { addDays, daysBetween, daysInMonth, startOfDay } from '../dates.ts';
import { order } from '../order.ts';
import type { Occurrence } from '../recurrence.ts';

export interface MonthGrid {
    year: number;
    month: number;
    firstDayOfWeek: number;
    start: number;
    end: number;
    weeks: number;
    days: number[];
    offset: number;
}

export interface MonthChip {
    key: string;
    occurrence: Occurrence;
    row: number;
    column: number;
    span: number;
    slot: number;
    first: boolean;
    last: boolean;
    multiDay: boolean;
}

export interface MonthLayout {
    chips: MonthChip[];
    overflow: number[];
    counts: number[];
}

interface Span {
    occurrence: Occurrence;
    first: number;
    last: number;
}

export function monthGrid(at: number, firstDayOfWeek: number, minWeeks = 0): MonthGrid {
    const date = new Date(at);
    const year = date.getFullYear();
    const month = date.getMonth();

    const offset = (new Date(year, month, 1).getDay() - firstDayOfWeek + 7) % 7;
    const start = new Date(year, month, 1 - offset).getTime();

    const monthEnd = new Date(year, month + 1, 1);
    const tail = (firstDayOfWeek - monthEnd.getDay() + 7) % 7;
    const weeks = Math.max(minWeeks, daysBetween(start, new Date(year, month + 1, 1 + tail).getTime()) / 7);

    return {
        year,
        month,
        firstDayOfWeek,
        start,
        end: addDays(start, weeks * 7),
        weeks,
        days: Array.from({ length: weeks * 7 }, (_, index) => addDays(start, index)),
        offset,
    };
}

export function cellOf(grid: MonthGrid, at: number): number {
    return daysBetween(grid.start, at);
}

export function inMonth(grid: MonthGrid, cell: number): boolean {
    return cell >= grid.offset && cell < grid.offset + daysInMonth(grid.year, grid.month);
}

export function layoutMonth(grid: MonthGrid, occurrences: Occurrence[], slotsPerCell: number): MonthLayout {
    const cells = grid.days.length;
    const taken: boolean[][] = Array.from({ length: cells }, () => []);
    const overflow: number[] = new Array<number>(cells).fill(0);
    const counts: number[] = new Array<number>(cells).fill(0);
    const chips: MonthChip[] = [];

    const multiDay: Span[] = [];
    const allDay: Span[] = [];
    const single: Span[] = [];

    for (const occurrence of occurrences) {
        const endAt = occurrence.endAt > occurrence.startAt && occurrence.endAt === startOfDay(occurrence.endAt)
            ? occurrence.endAt - 1
            : occurrence.endAt;

        const span = {
            occurrence,
            first: cellOf(grid, occurrence.startAt),
            last: cellOf(grid, endAt),
        };

        if (span.last < 0 || span.first >= cells) continue;

        (span.last > span.first ? multiDay : occurrence.allDay ? allDay : single).push(span);
    }

    const byStart = (a: Span, b: Span): number => order(a.occurrence, b.occurrence);

    multiDay.sort(byStart);
    allDay.sort(byStart);
    single.sort(byStart);

    for (const span of [...multiDay, ...allDay, ...single]) {
        const last = Math.min(cells - 1, span.last);
        let cell = Math.max(0, span.first);

        while (cell <= last) {
            const row = Math.floor(cell / 7);
            const stop = Math.min(last, row * 7 + 6);

            let slot = 0;
            while (slot < slotsPerCell) {
                let free = true;
                for (let index = cell; index <= stop && free; index++) free = taken[index]?.[slot] !== true;

                if (free) break;
                slot++;
            }

            for (let index = cell; index <= stop; index++) counts[index] = (counts[index] ?? 0) + 1;

            if (slot < slotsPerCell) {
                for (let index = cell; index <= stop; index++) taken[index]![slot] = true;

                chips.push({
                    key: `${span.occurrence.key}\uffff${cell}`,
                    occurrence: span.occurrence,
                    row,
                    column: cell % 7,
                    span: stop - cell + 1,
                    slot,
                    first: cell === span.first,
                    last: stop === span.last,
                    multiDay: span.last > span.first,
                });
            } else {
                for (let index = cell; index <= stop; index++) overflow[index] = (overflow[index] ?? 0) + 1;
            }

            cell = stop + 1;
        }
    }

    return { chips, overflow, counts };
}
