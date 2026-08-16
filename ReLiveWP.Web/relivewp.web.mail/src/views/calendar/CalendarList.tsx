import type { CalendarFolder } from "./useCalendars";

type Props = {
    calendars: CalendarFolder[],
    onToggle: (id: string) => void,
};

export default function CalendarList({ calendars, onToggle }: Props) {
    return (
        <div class="calendar-list">
            <h3>calendars</h3>

            <ul>
                {calendars.map((calendar) => (
                    <li key={calendar.id}>
                        <label class={calendar.enabled ? undefined : "off"} data-calendar={calendar.colour}>
                            <input
                                type="checkbox"
                                checked={calendar.enabled}
                                onChange={() => { onToggle(calendar.id); }}
                            />
                            <span class="calendar-name">{calendar.name}</span>
                        </label>
                    </li>
                ))}

                {calendars.length === 0 && <li class="note">None yet.</li>}
            </ul>
        </div>
    );
}
