import { readTask as readTaskItem } from '@relivewp/eas-client';
import { int, pick, readDate, tags, text, tri, type EasNode } from '@relivewp/eas-client/nodes';
import type { Importance, Recurrence, Task } from '@relivewp/eas-store';

import { epoch, readBody, readCategories, readSensitivity } from './shared.ts';

const { Tasks: T } = tags;

const IMPORTANCE: Readonly<Record<number, Importance>> = { 0: 'low', 1: 'normal', 2: 'high' };

// task recurrences aren't calendar ones, they add Regenerate and DeadOccur and carry their own start
function readRecurrence(node: EasNode | undefined): Recurrence | null {
    if (node === undefined) return null;

    return {
        type: int(node, T.Type) ?? 0,
        interval: int(node, T.Interval) ?? null,
        until: epoch(readDate(text(node, T.Until))),
        occurrences: int(node, T.Occurrences) ?? null,
        dayOfWeek: int(node, T.DayOfWeek) ?? null,
        dayOfMonth: int(node, T.DayOfMonth) ?? null,
        weekOfMonth: int(node, T.WeekOfMonth) ?? null,
        monthOfYear: int(node, T.MonthOfYear) ?? null,
        firstDayOfWeek: int(node, T.FirstDayOfWeek) ?? null,
        calendarType: int(node, T.CalendarType) ?? null,
        isLeapMonth: tri(node, T.IsLeapMonth) ?? null,
        regenerate: tri(node, T.Regenerate) ?? null,
        deadOccur: tri(node, T.DeadOccur) ?? null,
        startAt: epoch(readDate(text(node, T.Start))),
    };
}

export function readTask(data: EasNode, folderId: string, id: string): Task {
    const item = readTaskItem(data);

    return {
        id,
        folderId,
        subject: item.subject ?? '',
        complete: item.complete === 1,
        completedAt: epoch(item.dateCompleted),
        startAt: epoch(item.startDate),
        dueAt: epoch(item.dueDate),
        utcStartAt: epoch(item.utcStartDate),
        utcDueAt: epoch(item.utcDueDate),
        importance: IMPORTANCE[item.importance ?? 1] ?? 'normal',
        sensitivity: readSensitivity(item.sensitivity),
        reminderAt: epoch(item.reminderTime),
        reminderSet: item.reminderSet === 1,
        recurrence: readRecurrence(pick(data, T.Recurrence)),
        ordinalDate: epoch(item.ordinalDate),
        subOrdinalDate: item.subOrdinalDate ?? null,
        categories: readCategories(item.categories),
        body: readBody(item.body),
    };
}
