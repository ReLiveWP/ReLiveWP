import "./calendar.scss";

import {
    addDays,
    addMonths,
    expandAll,
    firstDayOfWeek,
    monthGrid,
    startOfDay,
    startOfWeek,
    type MonthGrid as Grid,
} from "@relivewp/calendar";
import type { Event } from "@relivewp/eas-store";
import { useTitle } from "@relivewp/ui";
import { useLocation } from "preact-iso";
import { useEffect, useMemo } from "preact/hooks";

import Page from "~/components/Page";
import { useMedia } from "~/hooks/useMedia";
import { useSync } from "~/state/sync";
import { calendarPath, isMode, parseDate, DEFAULT_MODE, MODES, type Mode } from "~/util/routes";
import CalendarList from "./CalendarList";
import { monthLabel, rangeLabel } from "./format";
import MonthGrid from "./MonthGrid";
import TimeGrid from "./TimeGrid";
import { useCalendars, type CalendarFolder } from "./useCalendars";
import { useEvents } from "./useEvents";

const ROWS = 6;

const WIDE = "(min-width: 1920px)";
const MEDIUM = "(min-width: 672px)";

type Span = {
    grid: Grid | null,
    days: number[],
    from: number,
    to: number,
};

function spanOf(mode: Mode, anchor: number, weekStart: number, dayCount: number): Span {
    if (mode === "month") {
        const grid = monthGrid(anchor, weekStart, ROWS);

        return { grid, days: [], from: grid.start, to: grid.end };
    }

    const start = mode === "week" ? startOfWeek(anchor, weekStart) : startOfDay(anchor);
    const length = mode === "week" ? 7 : dayCount;
    const days = Array.from({ length }, (_, index) => addDays(start, index));

    return { grid: null, days, from: start, to: addDays(start, length) };
}

function visible(events: Event[], calendars: CalendarFolder[]): Event[] {
    const off = new Set(calendars.filter((calendar) => !calendar.enabled).map((calendar) => calendar.id));

    return off.size === 0 ? events : events.filter((event) => !off.has(event.folderId));
}

type Props = {
    mode?: string,
    date?: string,
};

export default function Calendar({ mode: requested, date }: Props) {
    useTitle("calendar");

    const { route } = useLocation();
    const client = useSync().client.value;

    const { calendars, colours, error: folderError, toggle } = useCalendars(client);
    const { events, error } = useEvents(client, calendars.map((calendar) => calendar.id));

    const weekStart = useMemo(() => firstDayOfWeek(navigator.language), []);
    const wide = useMedia(WIDE);
    const medium = useMedia(MEDIUM);
    const dayCount = wide ? 3 : medium ? 2 : 1;

    const mode = isMode(requested) ? requested : DEFAULT_MODE;
    const parsed = parseDate(date);
    const anchor = parsed ?? startOfDay(Date.now());

    useEffect(() => {
        if (isMode(requested) && parsed !== null) return;

        route(calendarPath(mode, anchor), true);
    }, [requested, parsed, mode, anchor, route]);

    const span = useMemo(
        () => spanOf(mode, anchor, weekStart, dayCount),
        [mode, anchor, weekStart, dayCount],
    );

    const occurrences = useMemo(
        () => expandAll(visible(events, calendars), span.from, span.to),
        [events, calendars, span],
    );

    const step = (delta: number): void => {
        const next = mode === "month"
            ? addMonths(anchor, delta)
            : addDays(anchor, delta * (mode === "week" ? 7 : 1));

        route(calendarPath(mode, next), true);
    };

    const today = (): void => { route(calendarPath(mode, startOfDay(Date.now()))); };
    const show = (next: Mode): void => { route(calendarPath(next, anchor)); };

    const problem = folderError ?? error;

    return (
        <Page
            title={span.grid === null ? rangeLabel(span.days) : monthLabel(anchor)}
            sidebar={<CalendarList calendars={calendars} onToggle={toggle} />}
            actions={
                <div class="calendar-nav">
                    <div class="calendar-modes">
                        {MODES.map((option) => (
                            <button
                                key={option}
                                type="button"
                                class={option === mode ? "text-button on" : "text-button"}
                                aria-pressed={option === mode}
                                onClick={() => { show(option); }}
                            >
                                {option}
                            </button>
                        ))}
                    </div>

                    <div class="calendar-steps">
                        <button
                            type="button"
                            class="text-button"
                            onClick={() => { step(-1); }}
                            aria-label={`previous ${mode}`}
                        >
                            &lsaquo;
                        </button>
                        <button type="button" class="text-button" onClick={today}>today</button>
                        <button
                            type="button"
                            class="text-button"
                            onClick={() => { step(1); }}
                            aria-label={`next ${mode}`}
                        >
                            &rsaquo;
                        </button>
                    </div>
                </div>
            }
        >
            {problem !== null && <p class="error">{problem}</p>}
            {client === null && <p class="note">Connecting.</p>}

            {span.grid !== null
                ? <MonthGrid grid={span.grid} occurrences={occurrences} colours={colours} />
                : <TimeGrid days={span.days} occurrences={occurrences} colours={colours} />}
        </Page>
    );
}
