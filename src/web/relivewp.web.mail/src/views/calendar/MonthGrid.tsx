import "./month.scss";

import { inMonth, layoutMonth, sameDay, type MonthChip, type MonthGrid, type Occurrence } from "@relivewp/calendar";
import { useEffect, useMemo, useRef, useState } from "preact/hooks";

import { cellDate, clockTime, dayLabel, weekdayLabels } from "./format";

const DAY_HEADER = 20;
const CHIP_HEIGHT = 17;
const CHIP_GAP = 2;
const MORE_HEIGHT = 15;

type Props = {
    grid: MonthGrid,
    occurrences: Occurrence[],
    colours: Map<string, number>,
};

type Measured = { current: HTMLDivElement | null };

function useSlotsPerCell(body: Measured, weeks: number): number {
    const [slots, setSlots] = useState(3);

    useEffect(() => {
        const element = body.current;
        if (element === null) return;

        const measure = (): void => {
            const cell = element.clientHeight / weeks;
            const capacity = Math.floor((cell - DAY_HEADER) / CHIP_HEIGHT);
            const spare = cell - DAY_HEADER - capacity * CHIP_HEIGHT;

            setSlots(Math.max(0, spare >= MORE_HEIGHT ? capacity : capacity - 1));
        };

        const observer = new ResizeObserver(measure);
        observer.observe(element);
        measure();

        return () => observer.disconnect();
    }, [body, weeks]);

    return slots;
}

function offsets(row: number, weeks: number, above: number): Record<string, string> {
    return { top: `calc(${(row / weeks) * 100}% + ${above}px)` };
}

function columns(column: number, span: number): Record<string, string> {
    return { left: `${(column / 7) * 100}%`, width: `${(span / 7) * 100}%` };
}

type ChipProps = {
    chip: MonthChip,
    weeks: number,
    colour: number | undefined,
    outside: boolean,
};

function Chip({ chip, weeks, colour, outside }: ChipProps) {
    const { occurrence, row, column, span, slot, first, last, multiDay } = chip;

    const bar = multiDay || occurrence.allDay;
    const timed = !occurrence.allDay;
    const subject = occurrence.subject.length > 0 ? occurrence.subject : "(no subject)";

    return (
        <div
            class={["month-chip", bar ? "bar" : "", outside ? "outside" : ""].join(" ").trimEnd()}
            data-busy={occurrence.busy}
            data-calendar={colour}
            title={subject}
            style={{
                ...columns(column, span),
                ...offsets(row, weeks, DAY_HEADER + slot * CHIP_HEIGHT),
                height: `${CHIP_HEIGHT - CHIP_GAP}px`,
            }}
        >
            {!bar && <span class="month-chip-dot" />}
            {bar && timed && first && <span class="month-chip-time">{clockTime(occurrence.startAt)}</span>}

            <span class="month-chip-subject">{subject}</span>

            {bar && timed && last && <span class="month-chip-time">{clockTime(occurrence.endAt)}</span>}
            {!bar && <span class="month-chip-time">{clockTime(occurrence.startAt)}</span>}
        </div>
    );
}

export default function Month({ grid, occurrences, colours }: Props) {
    const body = useRef<HTMLDivElement | null>(null);
    const slots = useSlotsPerCell(body, grid.weeks);
    const today = Date.now();

    const { chips, overflow, counts } = useMemo(
        () => layoutMonth(grid, occurrences, slots),
        [grid, occurrences, slots],
    );

    // a bar reaching in from a neighbouring month still belongs to this one, so only a run that is
    // outside at both ends counts as outside
    const beyond = (cell: number, span = 1): boolean =>
        !inMonth(grid, cell) && !inMonth(grid, cell + span - 1);

    return (
        <div class="month">
            <div class="month-weekdays">
                {weekdayLabels(grid.firstDayOfWeek).map((label) => <span key={label}>{label}</span>)}
            </div>

            <div class="month-body" ref={body}>
                <div class="month-cells" style={{ gridTemplateRows: `repeat(${grid.weeks}, 1fr)` }}>
                    {grid.days.map((day, cell) => (
                        <div
                            key={day}
                            class={[
                                "month-cell",
                                inMonth(grid, cell) ? "" : "outside",
                                sameDay(day, today) ? "today" : "",
                            ].join(" ").trimEnd()}
                        >
                            <span class="month-date">{cellDate(day)}</span>
                        </div>
                    ))}
                </div>

                <div class="month-chips">
                    {chips.map((chip) => (
                        <Chip
                            key={chip.key}
                            chip={chip}
                            weeks={grid.weeks}
                            colour={colours.get(chip.occurrence.event.folderId)}
                            outside={beyond(chip.row * 7 + chip.column, chip.span)}
                        />
                    ))}

                    {overflow.map((hidden, cell) => hidden === 0 ? null : (
                        <div
                            key={`more${cell}`}
                            class={beyond(cell) ? "month-more outside" : "month-more"}
                            title={`${counts[cell] ?? 0} events on ${dayLabel(grid.days[cell] ?? 0)}`}
                            style={{
                                ...columns(cell % 7, 1),
                                ...offsets(Math.floor(cell / 7), grid.weeks, DAY_HEADER + slots * CHIP_HEIGHT),
                            }}
                        >
                            +{hidden}
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}
