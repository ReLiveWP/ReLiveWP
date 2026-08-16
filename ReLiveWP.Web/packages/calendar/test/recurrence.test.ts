import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { at, event, exception, rule, stamps } from './fixtures.ts';
import { expand } from '../src/index.ts';

const HOUR = 3_600_000;

const WEDNESDAY = 8;
const TUESDAY = 4;
const THURSDAY = 16;
const FRIDAY = 32;
const WEEKDAYS = 62;
const MON_WED_FRI = 42;

function series(startAt: number, patch: Parameters<typeof rule>[0], id = 'series') {
    return event({ id, startAt, endAt: startAt + HOUR, recurrence: rule(patch) });
}

describe('expand', () => {
    it('passes a plain event through when it touches the window', () => {
        const plain = event({ id: 'one', startAt: at(2026, 7, 12, 9), endAt: at(2026, 7, 12, 10) });

        assert.equal(expand(plain, at(2026, 7, 12), at(2026, 7, 13)).length, 1);
        assert.equal(expand(plain, at(2026, 7, 13), at(2026, 7, 14)).length, 0);
        assert.equal(expand(plain, at(2026, 7, 12, 9, 30), at(2026, 7, 12, 9, 45)).length, 1);
    });

    it('gives a series instance a key of its own', () => {
        const every = series(at(2026, 7, 12, 9), { type: 0 });
        const found = expand(every, at(2026, 7, 12), at(2026, 7, 14));

        assert.deepEqual(found.map((item) => item.key), [
            `series\uffff${at(2026, 7, 12, 9)}`,
            `series\uffff${at(2026, 7, 13, 9)}`,
        ]);
        assert.deepEqual(found.map((item) => item.seriesStartAt), [at(2026, 7, 12, 9), at(2026, 7, 13, 9)]);
    });
});

describe('expand daily', () => {
    it('counts a bounded series from its own start, not from the window', () => {
        const every = series(at(2026, 7, 12, 9), { type: 0, interval: 3, occurrences: 4 });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 1), at(2026, 8, 1))), [
            'Wed Aug 12 2026 09:00',
            'Sat Aug 15 2026 09:00',
            'Tue Aug 18 2026 09:00',
            'Fri Aug 21 2026 09:00',
        ]);
    });

    it('honours a day mask on a daily rule', () => {
        const every = series(at(2026, 7, 12, 9), { type: 0, interval: 1, dayOfWeek: WEEKDAYS });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 14), at(2026, 7, 18))), [
            'Fri Aug 14 2026 09:00',
            'Mon Aug 17 2026 09:00',
        ]);
    });

    // without the seek the loop would run out of steps somewhere around 2011
    it('reaches a window years past the start of an unbounded series', () => {
        const every = series(at(2000, 0, 1, 9), { type: 0, interval: 1 });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 12), at(2026, 7, 15))), [
            'Wed Aug 12 2026 09:00',
            'Thu Aug 13 2026 09:00',
            'Fri Aug 14 2026 09:00',
        ]);
    });
});

describe('expand weekly', () => {
    it('emits every named day but never one before the series starts', () => {
        const every = series(at(2026, 7, 12, 9), {
            type: 1, interval: 1, dayOfWeek: MON_WED_FRI, firstDayOfWeek: 1,
        });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 10), at(2026, 7, 24))), [
            'Wed Aug 12 2026 09:00',
            'Fri Aug 14 2026 09:00',
            'Mon Aug 17 2026 09:00',
            'Wed Aug 19 2026 09:00',
            'Fri Aug 21 2026 09:00',
        ]);
    });

    it('skips whole weeks on an interval', () => {
        const every = series(at(2026, 7, 12, 9), { type: 1, interval: 2, firstDayOfWeek: 1 });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 10), at(2026, 8, 14))), [
            'Wed Aug 12 2026 09:00',
            'Wed Aug 26 2026 09:00',
            'Wed Sep 09 2026 09:00',
        ]);
    });

    it('stops at until, inclusively', () => {
        const every = series(at(2026, 7, 12, 9), { type: 1, interval: 1, until: at(2026, 7, 26, 9) });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 10), at(2026, 8, 14))), [
            'Wed Aug 12 2026 09:00',
            'Wed Aug 19 2026 09:00',
            'Wed Aug 26 2026 09:00',
        ]);
    });
});

describe('expand monthly and yearly', () => {
    it('clamps the thirty first onto short months instead of skipping them', () => {
        const every = series(at(2026, 0, 31, 9), { type: 2, interval: 1, dayOfMonth: 31 });

        assert.deepEqual(stamps(expand(every, at(2026, 0, 1), at(2026, 5, 1))), [
            'Sat Jan 31 2026 09:00',
            'Sat Feb 28 2026 09:00',
            'Tue Mar 31 2026 09:00',
            'Thu Apr 30 2026 09:00',
            'Sun May 31 2026 09:00',
        ]);
    });

    it('finds the nth weekday of the month', () => {
        const every = series(at(2026, 7, 11, 9), {
            type: 3, interval: 1, dayOfWeek: TUESDAY, weekOfMonth: 2,
        });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 1), at(2026, 10, 1))), [
            'Tue Aug 11 2026 09:00',
            'Tue Sep 08 2026 09:00',
            'Tue Oct 13 2026 09:00',
        ]);
    });

    it('reads a week of five as the last one, however many the month has', () => {
        const every = series(at(2026, 7, 28, 9), {
            type: 3, interval: 1, dayOfWeek: FRIDAY, weekOfMonth: 5,
        });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 1), at(2026, 10, 1))), [
            'Fri Aug 28 2026 09:00',
            'Fri Sep 25 2026 09:00',
            'Fri Oct 30 2026 09:00',
        ]);
    });

    it('repeats a fixed date every year', () => {
        const every = series(at(2026, 11, 25, 9), {
            type: 5, interval: 1, monthOfYear: 12, dayOfMonth: 25,
        });

        assert.deepEqual(stamps(expand(every, at(2026, 11, 1), at(2029, 0, 1))), [
            'Fri Dec 25 2026 09:00',
            'Sat Dec 25 2027 09:00',
            'Mon Dec 25 2028 09:00',
        ]);
    });

    it('repeats an nth weekday of a named month every year', () => {
        const every = series(at(2026, 10, 26, 9), {
            type: 6, interval: 1, monthOfYear: 11, weekOfMonth: 4, dayOfWeek: THURSDAY,
        });

        assert.deepEqual(stamps(expand(every, at(2026, 10, 1), at(2027, 11, 1))), [
            'Thu Nov 26 2026 09:00',
            'Thu Nov 25 2027 09:00',
        ]);
    });

    it('picks the last day of the month out of an every-day mask', () => {
        const every = series(at(2026, 7, 31, 9), {
            type: 3, interval: 1, dayOfWeek: 127, weekOfMonth: 5,
        });

        assert.deepEqual(stamps(expand(every, at(2026, 7, 1), at(2026, 9, 1))), [
            'Mon Aug 31 2026 09:00',
            'Wed Sep 30 2026 09:00',
        ]);
    });
});

describe('expand exceptions', () => {
    const weekly = { type: 1, interval: 1, dayOfWeek: WEDNESDAY, firstDayOfWeek: 1 } as const;

    function withExceptions(...list: ReturnType<typeof exception>[]) {
        return event({
            id: 'series',
            startAt: at(2026, 7, 12, 9),
            endAt: at(2026, 7, 12, 10),
            recurrence: rule(weekly),
            exceptions: list,
        });
    }

    it('leaves a gap where an instance was deleted', () => {
        const every = withExceptions(exception({ exceptionStartAt: at(2026, 7, 19, 9), deleted: true }));

        assert.deepEqual(stamps(expand(every, at(2026, 7, 10), at(2026, 8, 1))), [
            'Wed Aug 12 2026 09:00',
            'Wed Aug 26 2026 09:00',
        ]);
    });

    it('moves an instance and takes its overrides', () => {
        const every = withExceptions(exception({
            exceptionStartAt: at(2026, 7, 19, 9),
            startAt: at(2026, 7, 19, 14),
            endAt: at(2026, 7, 19, 15),
            subject: 'moved',
            location: 'the other room',
        }));

        const found = expand(every, at(2026, 7, 10), at(2026, 8, 1));

        assert.deepEqual(stamps(found), [
            'Wed Aug 12 2026 09:00',
            'Wed Aug 19 2026 14:00',
            'Wed Aug 26 2026 09:00',
        ]);
        assert.deepEqual(found.map((item) => item.subject), ['event', 'moved', 'event']);
        assert.deepEqual(found.map((item) => item.location), [null, 'the other room', null]);
        assert.deepEqual(found.map((item) => item.exception), [false, true, false]);
    });

    // the slot it belongs to sits outside the window, so only a second pass over the exceptions
    // themselves can find it
    it('drags an instance in from a slot the window never generated', () => {
        const every = withExceptions(exception({
            exceptionStartAt: at(2026, 7, 26, 9),
            startAt: at(2026, 7, 20, 14),
            endAt: at(2026, 7, 20, 15),
        }));

        assert.deepEqual(stamps(expand(every, at(2026, 7, 10), at(2026, 7, 24))), [
            'Wed Aug 12 2026 09:00',
            'Wed Aug 19 2026 09:00',
            'Thu Aug 20 2026 14:00',
        ]);
    });

    it('keeps an instance out when the exception moves it away', () => {
        const every = withExceptions(exception({
            exceptionStartAt: at(2026, 7, 19, 9),
            startAt: at(2026, 8, 19, 9),
            endAt: at(2026, 8, 19, 10),
        }));

        assert.deepEqual(stamps(expand(every, at(2026, 7, 10), at(2026, 7, 24))), [
            'Wed Aug 12 2026 09:00',
        ]);
    });
});

describe('expand across a daylight saving transition', () => {
    it('holds the wall clock rather than the elapsed offset', () => {
        const every = series(at(2026, 2, 27, 9, 30), { type: 0, interval: 1 });

        assert.deepEqual(stamps(expand(every, at(2026, 2, 27), at(2026, 3, 1))), [
            'Fri Mar 27 2026 09:30',
            'Sat Mar 28 2026 09:30',
            'Sun Mar 29 2026 09:30',
            'Mon Mar 30 2026 09:30',
            'Tue Mar 31 2026 09:30',
        ]);
    });
});
