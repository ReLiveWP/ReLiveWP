import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { at, occurrence } from './fixtures.ts';
import { cellOf, inMonth, layoutMonth, monthGrid } from '../src/index.ts';

const AUGUST = monthGrid(at(2026, 7, 12), 1);

function boxes(chips: { occurrence: { key: string }, row: number, column: number, span: number, slot: number }[]) {
    return chips.map((chip) => [chip.occurrence.key, chip.row, chip.column, chip.span, chip.slot] as const);
}

describe('monthGrid', () => {
    it('runs from the week start before the first of the month to the one after the last', () => {
        assert.equal(AUGUST.start, at(2026, 6, 27));
        assert.equal(AUGUST.end, at(2026, 8, 7));
        assert.equal(AUGUST.weeks, 6);
        assert.equal(AUGUST.offset, 5);
        assert.equal(AUGUST.days.length, 42);
    });

    it('shifts with the week start', () => {
        const sunday = monthGrid(at(2026, 7, 12), 0);

        assert.equal(sunday.start, at(2026, 6, 26));
        assert.equal(sunday.offset, 6);
        assert.equal(sunday.weeks, 6);
    });

    it('gives february its natural four rows and pads them on request', () => {
        assert.equal(monthGrid(at(2026, 1, 10), 0).weeks, 4);
        assert.equal(monthGrid(at(2026, 1, 10), 0).end, at(2026, 2, 1));

        const padded = monthGrid(at(2026, 1, 10), 0, 6);

        assert.equal(padded.weeks, 6);
        assert.equal(padded.end, at(2026, 2, 15));
    });

    it('places a date in a cell and says whether it belongs to the month', () => {
        assert.equal(cellOf(AUGUST, at(2026, 7, 1)), 5);
        assert.equal(cellOf(AUGUST, at(2026, 7, 12, 23, 59)), 16);
        assert.equal(inMonth(AUGUST, 4), false);
        assert.equal(inMonth(AUGUST, 5), true);
        assert.equal(inMonth(AUGUST, 35), true);
        assert.equal(inMonth(AUGUST, 36), false);
    });
});

describe('layoutMonth', () => {
    it('puts a single day event in one cell', () => {
        const { chips, counts, overflow } = layoutMonth(
            AUGUST, [occurrence('a', at(2026, 7, 12, 9), at(2026, 7, 12, 10))], 3,
        );

        assert.deepEqual(boxes(chips), [['a', 2, 2, 1, 0]]);
        assert.deepEqual([chips[0]?.first, chips[0]?.last, chips[0]?.multiDay], [true, true, false]);
        assert.equal(counts[16], 1);
        assert.equal(overflow[16], 0);
    });

    // a bar that ran across two rows would have to leave the grid and come back, so it becomes one
    // chip per week instead
    it('breaks a spanning event at the week boundary', () => {
        const { chips } = layoutMonth(
            AUGUST, [occurrence('a', at(2026, 7, 7, 9), at(2026, 7, 10, 12))], 3,
        );

        assert.deepEqual(boxes(chips), [['a', 1, 4, 3, 0], ['a', 2, 0, 1, 0]]);
        assert.deepEqual(chips.map((chip) => [chip.first, chip.last]), [[true, false], [false, true]]);
        assert.deepEqual(chips.map((chip) => chip.multiDay), [true, true]);
        assert.notEqual(chips[0]?.key, chips[1]?.key);
    });

    it('holds one slot across every cell a bar covers', () => {
        const { chips } = layoutMonth(AUGUST, [
            occurrence('single', at(2026, 7, 12, 9), at(2026, 7, 12, 10)),
            occurrence('bar', at(2026, 7, 11, 9), at(2026, 7, 13, 10)),
        ], 3);

        assert.deepEqual(boxes(chips), [['bar', 2, 1, 3, 0], ['single', 2, 2, 1, 1]]);
    });

    it('stacks overlapping bars rather than crossing them', () => {
        const { chips } = layoutMonth(AUGUST, [
            occurrence('first', at(2026, 7, 10, 9), at(2026, 7, 12, 10)),
            occurrence('second', at(2026, 7, 11, 9), at(2026, 7, 13, 10)),
        ], 3);

        assert.deepEqual(boxes(chips), [['first', 2, 0, 3, 0], ['second', 2, 1, 3, 1]]);
    });

    it('counts what did not fit instead of drawing it', () => {
        const crowd = ['a', 'b', 'c', 'd'].map((key) =>
            occurrence(key, at(2026, 7, 12, 9), at(2026, 7, 12, 10)));

        const { chips, counts, overflow } = layoutMonth(AUGUST, crowd, 2);

        assert.deepEqual(chips.map((chip) => chip.occurrence.key), ['a', 'b']);
        assert.equal(counts[16], 4);
        assert.equal(overflow[16], 2);
    });

    it('draws nothing at all when the cell has no room', () => {
        const { chips, overflow } = layoutMonth(
            AUGUST, [occurrence('a', at(2026, 7, 12, 9), at(2026, 7, 12, 10))], 0,
        );

        assert.deepEqual(chips, []);
        assert.equal(overflow[16], 1);
    });

    it('stops an event that lands on midnight claiming the next day', () => {
        const { chips } = layoutMonth(
            AUGUST, [occurrence('a', at(2026, 7, 12, 9), at(2026, 7, 13))], 3,
        );

        assert.deepEqual(boxes(chips), [['a', 2, 2, 1, 0]]);
        assert.equal(chips[0]?.multiDay, false);
    });

    it('clips an event that starts before the grid and ends after it', () => {
        const { chips } = layoutMonth(
            AUGUST, [occurrence('a', at(2026, 6, 1), at(2026, 9, 1))], 3,
        );

        assert.deepEqual(chips.map((chip) => [chip.row, chip.column, chip.span]), [
            [0, 0, 7], [1, 0, 7], [2, 0, 7], [3, 0, 7], [4, 0, 7], [5, 0, 7],
        ]);
        assert.deepEqual(chips.map((chip) => [chip.first, chip.last]), [
            [false, false], [false, false], [false, false],
            [false, false], [false, false], [false, false],
        ]);
    });

    it('ignores an event that misses the grid entirely', () => {
        const { chips, counts } = layoutMonth(
            AUGUST, [occurrence('a', at(2026, 9, 12, 9), at(2026, 9, 12, 10))], 3,
        );

        assert.deepEqual(chips, []);
        assert.deepEqual(counts.filter((count) => count > 0), []);
    });
});
