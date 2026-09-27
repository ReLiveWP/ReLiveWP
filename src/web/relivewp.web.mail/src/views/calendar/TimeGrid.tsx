import "./timegrid.scss";

import {
    MINUTES_PER_DAY,
    layoutDay,
    minutesIntoDay,
    sameDay,
    type Occurrence,
    type TimedBox,
} from "@relivewp/calendar";
import { useEffect, useLayoutEffect, useMemo, useRef, useState } from "preact/hooks";

import { exactTime, hourLabels, weekdayOf } from "./format";

const HOURS = hourLabels();
const WORKING_TOP = 8 / 24;
const SCROLL_PAD = 12;
const TICK_MS = 60_000;

type Props = {
    days: number[],
    occurrences: Occurrence[],
    colours: Map<string, number>,
};

function useNow(): number {
    const [now, setNow] = useState(() => Date.now());

    useEffect(() => {
        let timer: ReturnType<typeof setInterval> | undefined;

        const stop = (): void => {
            clearInterval(timer);
            timer = undefined;
        };

        const start = (): void => {
            setNow(Date.now());
            timer = setInterval(() => { setNow(Date.now()); }, TICK_MS);
        };

        const onVisibility = (): void => {
            stop();
            if (!document.hidden) start();
        };

        if (!document.hidden) start();
        document.addEventListener("visibilitychange", onVisibility);

        return () => {
            stop();
            document.removeEventListener("visibilitychange", onVisibility);
        };
    }, []);

    return now;
}

function label(occurrence: Occurrence): string {
    return occurrence.subject.length > 0 ? occurrence.subject : "(no subject)";
}

function described(occurrence: Occurrence): string {
    const when = `${exactTime(occurrence.startAt)} – ${exactTime(occurrence.endAt)}`;

    return occurrence.location === null ? `${when}  ${label(occurrence)}` : `${when}  ${label(occurrence)}, ${occurrence.location}`;
}

function Box({ box, colour }: { box: TimedBox, colour: number | undefined }) {
    const { occurrence } = box;

    return (
        <div
            class={["timegrid-box", box.short ? "short" : "", box.abuts ? "abuts" : ""].join(" ").trimEnd()}
            data-busy={occurrence.busy}
            data-calendar={colour}
            title={described(occurrence)}
            style={{
                top: `${box.top * 100}%`,
                height: `${box.height * 100}%`,
                left: `${box.left * 100}%`,
                width: `${box.width * 100}%`,
            }}
        >
            <span class="timegrid-box-subject">{label(occurrence)}</span>
            {occurrence.location !== null && <span class="timegrid-box-where">{occurrence.location}</span>}
        </div>
    );
}

export default function TimeGrid({ days, occurrences, colours }: Props) {
    const scroll = useRef<HTMLDivElement | null>(null);
    const now = useNow();

    const layouts = useMemo(() => days.map((day) => layoutDay(occurrences, day)), [days, occurrences]);
    const strip = layouts.some((layout) => layout.allDay.length > 0);
    const columns = { gridTemplateColumns: `repeat(${days.length}, minmax(0, 1fr))` };

    useLayoutEffect(() => {
        const element = scroll.current;
        if (element === null) return;

        const target = Math.min(WORKING_TOP, ...layouts.flatMap((layout) => layout.boxes.map((box) => box.top)));

        element.scrollTop = Math.max(0, target * element.scrollHeight - SCROLL_PAD);
    }, [days.length]);

    return (
        <div class="timegrid">
            <div class="timegrid-row timegrid-head">
                <div class="timegrid-gutter" />
                <div class="timegrid-columns" style={columns}>
                    {days.map((day) => (
                        <div key={day} class={sameDay(day, now) ? "timegrid-heading today" : "timegrid-heading"}>
                            <span class="timegrid-weekday">{weekdayOf(day)}</span>
                            <span class="timegrid-date">{new Date(day).getDate()}</span>
                        </div>
                    ))}
                </div>
            </div>

            {strip && (
                <div class="timegrid-row timegrid-allday">
                    <div class="timegrid-gutter">all day</div>
                    <div class="timegrid-columns" style={columns}>
                        {layouts.map((layout) => (
                            <div key={layout.day} class="timegrid-allday-cell">
                                {layout.allDay.map((occurrence) => (
                                    <div
                                        key={occurrence.key}
                                        class="timegrid-allday-chip"
                                        data-busy={occurrence.busy}
                                        data-calendar={colours.get(occurrence.event.folderId)}
                                        title={label(occurrence)}
                                    >
                                        {label(occurrence)}
                                    </div>
                                ))}
                            </div>
                        ))}
                    </div>
                </div>
            )}

            <div class="timegrid-scroll" ref={scroll}>
                <div class="timegrid-row timegrid-body">
                    <div class="timegrid-gutter">
                        {HOURS.map((hour, index) => (
                            <div key={index} class="timegrid-hour">{hour}</div>
                        ))}
                    </div>

                    <div class="timegrid-columns" style={columns}>
                        {layouts.map((layout) => (
                            <div key={layout.day} class="timegrid-column">
                                {layout.boxes.map((box) => (
                                    <Box
                                        key={box.occurrence.key}
                                        box={box}
                                        colour={colours.get(box.occurrence.event.folderId)}
                                    />
                                ))}

                                {sameDay(layout.day, now) && (
                                    <div
                                        class="timegrid-now"
                                        style={{ top: `${(minutesIntoDay(now) / MINUTES_PER_DAY) * 100}%` }}
                                    />
                                )}
                            </div>
                        ))}
                    </div>
                </div>
            </div>
        </div>
    );
}
