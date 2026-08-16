const RELATIVE = new Intl.RelativeTimeFormat(undefined, { numeric: "auto", style: "narrow" });

const UNITS: [Intl.RelativeTimeFormatUnit, number][] = [
    ["day", 86_400_000],
    ["hour", 3_600_000],
    ["minute", 60_000],
];

export function ago(at: number | string): string {
    const at_ms = typeof at === "string" ? Date.parse(at) : at;
    if (Number.isNaN(at_ms)) return "";

    const elapsed = Date.now() - at_ms;

    for (const [unit, size] of UNITS)
        if (elapsed >= size) return RELATIVE.format(-Math.floor(elapsed / size), unit);

    return "just now";
}
