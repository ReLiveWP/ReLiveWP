import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { ZONED, at } from './fixtures.ts';
import {
    addDays,
    addMonths,
    daysBetween,
    daysInMonth,
    firstDayOfWeek,
    minutesIntoDay,
    onDate,
    startOfDay,
    startOfWeek,
    weekdayColumn,
} from '../src/index.ts';

const SPRING = at(2026, 2, 29);
const AUTUMN = at(2026, 9, 25);

describe('dates', () => {
    it('walks calendar days rather than fixed spans', () => {
        assert.equal(startOfDay(addDays(at(2026, 7, 12, 9, 30), 1)), at(2026, 7, 13));
        assert.equal(startOfDay(addDays(at(2026, 7, 31, 23, 59), 1)), at(2026, 8, 1));
        assert.equal(startOfDay(addDays(at(2026, 0, 1), -1)), at(2025, 11, 31));
    });

    it('clamps a month step onto months that are too short for the day', () => {
        assert.equal(startOfDay(addMonths(at(2026, 0, 31), 1)), at(2026, 1, 28));
        assert.equal(startOfDay(addMonths(at(2026, 0, 31), 2)), at(2026, 2, 31));
        assert.equal(daysInMonth(2024, 1), 29);
    });

    it('finds the column and the start of a week for either week start', () => {
        assert.equal(weekdayColumn(at(2026, 7, 12), 1), 2);
        assert.equal(weekdayColumn(at(2026, 7, 12), 0), 3);
        assert.equal(startOfWeek(at(2026, 7, 12, 14, 0), 1), at(2026, 7, 10));
        assert.equal(startOfWeek(at(2026, 7, 12, 14, 0), 0), at(2026, 7, 9));
    });

    it('carries a wall clock onto another date', () => {
        assert.equal(onDate(2027, 1, 3, at(2026, 7, 12, 9, 30)), at(2027, 1, 3, 9, 30));
    });

    it('maps the getWeekInfo week start onto a getDay index', () => {
        assert.equal(firstDayOfWeek('en-GB'), 1);
        assert.equal(firstDayOfWeek('en-US'), 0);
        assert.equal(firstDayOfWeek('ar-EG'), 6);
    });

    it('falls back to a region table when getWeekInfo is missing', () => {
        const proto = Intl.Locale.prototype as { getWeekInfo?: unknown };
        const saved = proto.getWeekInfo;
        delete proto.getWeekInfo;

        try {
            assert.equal(firstDayOfWeek('en-GB'), 1);
            assert.equal(firstDayOfWeek('en-US'), 0);
            assert.equal(firstDayOfWeek('ar-EG'), 6);
            assert.equal(firstDayOfWeek('cy'), 1);
        } finally {
            if (saved !== undefined) proto.getWeekInfo = saved;
        }
    });
});

describe('dates across a daylight saving transition', { skip: ZONED ? false : 'TZ override unavailable' }, () => {
    it('has a 23 hour spring day and a 25 hour autumn day', () => {
        assert.equal((addDays(SPRING, 1) - SPRING) / 3_600_000, 23);
        assert.equal((addDays(AUTUMN, 1) - AUTUMN) / 3_600_000, 25);
    });

    // the whole reason daysBetween rounds: 23 hours over a 24 hour divisor floors to zero
    it('still counts one day between the two midnights', () => {
        assert.equal(daysBetween(SPRING, addDays(SPRING, 1)), 1);
        assert.equal(daysBetween(AUTUMN, addDays(AUTUMN, 1)), 1);
        assert.equal(daysBetween(at(2026, 2, 1), at(2026, 3, 1)), 31);
    });

    it('keeps the wall clock when stepping a day, a week and a month', () => {
        assert.equal(minutesIntoDay(addDays(at(2026, 2, 28, 9, 30), 1)), 570);
        assert.equal(minutesIntoDay(addDays(at(2026, 2, 28, 9, 30), 7)), 570);
        assert.equal(minutesIntoDay(addMonths(at(2026, 2, 28, 9, 30), 1)), 570);
    });

    it('reads a nominal offset down the column, never elapsed time', () => {
        assert.equal(minutesIntoDay(at(2026, 2, 29, 9, 30)), 570);
        assert.equal(minutesIntoDay(at(2026, 9, 25, 23, 0)), 1380);
    });
});
