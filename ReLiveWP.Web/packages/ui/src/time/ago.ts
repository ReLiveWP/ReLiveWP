const RELATIVE = "RelativeTimeFormat" in Intl
    ? new Intl.RelativeTimeFormat(undefined, { numeric: "auto", style: "narrow" })
    : null;

export const UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
    ["day", 86_400_000],
    ["hour", 3_600_000],
    ["minute", 60_000],
];

function plain(value: number, unit: Intl.RelativeTimeFormatUnit): string {
    const count = Math.abs(value);
    const noun = count === 1 ? unit : `${unit}s`;

    return value < 0 ? `${count} ${noun} ago` : `in ${count} ${noun}`;
}

export function relative(value: number, unit: Intl.RelativeTimeFormatUnit): string {
    return RELATIVE ? RELATIVE.format(value, unit) : plain(value, unit);
}

export function ago(at: number | string): string {
    const at_ms = typeof at === "string" ? Date.parse(at) : at;
    if (Number.isNaN(at_ms)) return "";

    const elapsed = Date.now() - at_ms;

    for (const [unit, size] of UNITS)
        if (elapsed >= size) return relative(-Math.floor(elapsed / size), unit);

    return "just now";
}
