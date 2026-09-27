export type DateFormat = 'compact' | 'extended';

const pad = (value: number, width = 2) => String(value).padStart(width, '0');

export function writeDate(value: Date | undefined, format: DateFormat): string | undefined {
    if (value === undefined || Number.isNaN(value.getTime())) return undefined;

    const date = `${value.getUTCFullYear()}${pad(value.getUTCMonth() + 1)}${pad(value.getUTCDate())}`;
    const time = `${pad(value.getUTCHours())}${pad(value.getUTCMinutes())}${pad(value.getUTCSeconds())}`;

    if (format === 'compact') return `${date}T${time}Z`;

    return `${value.getUTCFullYear()}-${pad(value.getUTCMonth() + 1)}-${pad(value.getUTCDate())}`
        + `T${pad(value.getUTCHours())}:${pad(value.getUTCMinutes())}:${pad(value.getUTCSeconds())}`
        + `.${pad(value.getUTCMilliseconds(), 3)}Z`;
}

const COMPACT =/^(\d{4})(\d{2})(\d{2})T(\d{2})(\d{2})(\d{2})Z$/;

export function readDate(value: string | undefined): Date | undefined {
    if (value === undefined || value === '') return undefined;

    const compact = COMPACT.exec(value);
    if (compact) {
        const [, y, mo, d, h, mi, s] = compact;
        return new Date(Date.UTC(Number(y), Number(mo) - 1, Number(d), Number(h), Number(mi), Number(s)));
    }

    const parsed = Date.parse(value);
    return Number.isNaN(parsed) ? undefined : new Date(parsed);
}
