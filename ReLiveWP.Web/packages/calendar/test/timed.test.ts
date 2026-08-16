import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { ZONED, at, occurrence } from './fixtures.ts';
import { layoutDay } from '../src/index.ts';

const DAY = at(2026, 7, 12);

function lanes(boxes: { occurrence: { key: string }, left: number, width: number }[]) {
    return boxes.map((box) => [box.occurrence.key, box.left, box.width] as const);
}

function span(key: string, fromHour: number, fromMinute: number, toHour: number, toMinute: number) {
    return occurrence(key, at(2026, 7, 12, fromHour, fromMinute), at(2026, 7, 12, toHour, toMinute));
}

describe('layoutDay', () => {
    it('splits a column between two events that overlap', () => {
        const { boxes } = layoutDay([span('a', 9, 0, 10, 0), span('b', 9, 30, 10, 30)], DAY);

        assert.deepEqual(lanes(boxes), [['a', 0, 0.5], ['b', 0.5, 0.5]]);
    });

    it('reads a position off a fixed 1440 minute scale', () => {
        const [box] = layoutDay([span('a', 9, 30, 10, 30)], DAY).boxes;

        assert.equal(box?.top, 570 / 1440);
        assert.equal(box?.height, 60 / 1440);
    });

    // the thirty minute floor governs collision, not just height, so the second event cannot land
    // on top of the first
    it('gives back to back five minute events a lane each', () => {
        const { boxes } = layoutDay([span('a', 9, 0, 9, 5), span('b', 9, 5, 9, 10)], DAY);

        assert.deepEqual(lanes(boxes), [['a', 0, 0.5], ['b', 0.5, 0.5]]);
        assert.deepEqual(boxes.map((box) => [box.short, box.height]), [[true, 30 / 1440], [true, 30 / 1440]]);
    });

    it('marks an event that starts exactly where another finishes', () => {
        const { boxes } = layoutDay([span('a', 9, 0, 10, 0), span('b', 10, 0, 11, 0)], DAY);

        assert.deepEqual(boxes.map((box) => box.abuts), [false, true]);
    });

    it('starts a fresh cluster after a gap so the morning cannot squash the afternoon', () => {
        const { boxes } = layoutDay(
            [span('a', 9, 0, 10, 0), span('b', 9, 0, 10, 0), span('c', 14, 0, 15, 0)],
            DAY,
        );

        assert.deepEqual(lanes(boxes), [['a', 0, 0.5], ['b', 0.5, 0.5], ['c', 0, 1]]);
    });

    // d takes lane 1 once b is done and spreads into lane 2, which c vacated at 09:10
    it('widens an event rightwards while the lanes beside it are free', () => {
        const { boxes } = layoutDay([
            span('a', 9, 0, 10, 0),
            span('b', 9, 0, 9, 15),
            span('c', 9, 0, 9, 10),
            span('d', 9, 30, 10, 0),
        ], DAY);

        assert.deepEqual(lanes(boxes), [
            ['a', 0, 1 / 3],
            ['b', 1 / 3, 1 / 3],
            ['c', 2 / 3, 1 / 3],
            ['d', 1 / 3, 2 / 3],
        ]);
    });

    it('sends all day and whole-day-spanning events to the strip', () => {
        const whole = occurrence('all', at(2026, 7, 12), at(2026, 7, 13), { allDay: true });
        const long = occurrence('long', at(2026, 7, 12, 22), at(2026, 7, 13, 23));
        const { boxes, allDay } = layoutDay([whole, long, span('a', 9, 0, 10, 0)], DAY);

        assert.deepEqual(allDay.map((item) => item.key), ['all', 'long']);
        assert.deepEqual(boxes.map((box) => box.occurrence.key), ['a']);
    });

    it('clips an overnight event into both days rather than banishing it', () => {
        const night = occurrence('night', at(2026, 7, 12, 22), at(2026, 7, 13, 2));

        const first = layoutDay([night], DAY).boxes[0];
        const second = layoutDay([night], at(2026, 7, 13)).boxes[0];

        assert.deepEqual([first?.top, first?.height], [1320 / 1440, 120 / 1440]);
        assert.deepEqual([second?.top, second?.height], [0, 120 / 1440]);
    });

    it('leaves a day with nothing on it empty', () => {
        const { boxes, allDay } = layoutDay([span('a', 9, 0, 10, 0)], at(2026, 7, 13));

        assert.deepEqual(boxes, []);
        assert.deepEqual(allDay, []);
    });
});

describe('layoutDay across a daylight saving transition', { skip: ZONED ? false : 'TZ override unavailable' }, () => {
    it('keeps wall clock positions on a 23 hour day', () => {
        const short = occurrence('a', at(2026, 2, 29, 9, 30), at(2026, 2, 29, 10, 30));
        const [box] = layoutDay([short], at(2026, 2, 29)).boxes;

        assert.equal(box?.top, 570 / 1440);
        assert.equal(box?.height, 60 / 1440);
    });

    it('keeps wall clock positions on a 25 hour day', () => {
        const late = occurrence('a', at(2026, 9, 25, 23, 0), at(2026, 9, 25, 23, 30));
        const [box] = layoutDay([late], at(2026, 9, 25)).boxes;

        assert.equal(box?.top, 1380 / 1440);
        assert.equal(box?.height, 30 / 1440);
    });

    it('still fills a whole column with an event that runs the length of the day', () => {
        const all = occurrence('a', at(2026, 9, 25), at(2026, 9, 26));
        const { boxes, allDay } = layoutDay([all], at(2026, 9, 25));

        assert.deepEqual(boxes, []);
        assert.deepEqual(allDay.map((item) => item.key), ['a']);
    });
});
