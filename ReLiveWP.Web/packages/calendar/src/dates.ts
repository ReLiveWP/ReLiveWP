export const MINUTE = 60_000;
export const HOUR = 3_600_000;
export const DAY = 86_400_000;
export const MINUTES_PER_DAY = 1440;

export function startOfDay(at: number): number {
    const date = new Date(at);

    return new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
}

export function addDays(at: number, days: number): number {
    const date = new Date(at);

    return new Date(
        date.getFullYear(), date.getMonth(), date.getDate() + days,
        date.getHours(), date.getMinutes(), date.getSeconds(), date.getMilliseconds(),
    ).getTime();
}

export function addMonths(at: number, months: number): number {
    const date = new Date(at);
    const year = date.getFullYear();
    const month = date.getMonth() + months;

    return new Date(
        year, month, Math.min(date.getDate(), daysInMonth(year, month)),
        date.getHours(), date.getMinutes(), date.getSeconds(), date.getMilliseconds(),
    ).getTime();
}

export function daysInMonth(year: number, month: number): number {
    return new Date(year, month + 1, 0).getDate();
}

// round, not floor: a dst transition leaves 23 or 25 hours between two midnights and floor loses a
// whole day on one of them
export function daysBetween(from: number, to: number): number {
    return Math.round((startOfDay(to) - startOfDay(from)) / DAY);
}

export function sameDay(a: number, b: number): boolean {
    const first = new Date(a);
    const second = new Date(b);

    return first.getDate() === second.getDate()
        && first.getMonth() === second.getMonth()
        && first.getFullYear() === second.getFullYear();
}

// a nominal offset on a fixed 1440-minute scale, never elapsed time: on a 23- or 25-hour day an
// event at 09:30 still belongs nine and a half hours down the column
export function minutesIntoDay(at: number): number {
    const date = new Date(at);

    return date.getHours() * 60 + date.getMinutes();
}

export function weekdayColumn(at: number, firstDayOfWeek: number): number {
    return (new Date(at).getDay() - firstDayOfWeek + 7) % 7;
}

export function startOfWeek(at: number, firstDayOfWeek: number): number {
    return addDays(startOfDay(at), -weekdayColumn(at, firstDayOfWeek));
}

export function onDate(year: number, month: number, day: number, template: number): number {
    const time = new Date(template);

    return new Date(
        year, month, day,
        time.getHours(), time.getMinutes(), time.getSeconds(), time.getMilliseconds(),
    ).getTime();
}

const SUNDAY_REGIONS = new Set([
    'AG', 'AR', 'AS', 'AU', 'BD', 'BR', 'BS', 'BT', 'BW', 'BZ', 'CA', 'CN', 'CO', 'DM', 'DO', 'ET',
    'GT', 'GU', 'HK', 'HN', 'ID', 'IL', 'IN', 'JM', 'JP', 'KE', 'KH', 'KR', 'LA', 'MH', 'MM', 'MO',
    'MT', 'MX', 'MZ', 'NI', 'NP', 'PA', 'PE', 'PH', 'PK', 'PR', 'PY', 'SA', 'SG', 'SV', 'TH', 'TT',
    'TW', 'UM', 'US', 'VE', 'VI', 'WS', 'YE', 'ZA', 'ZW',
]);

const SATURDAY_REGIONS = new Set([
    'AE', 'AF', 'BH', 'DJ', 'DZ', 'EG', 'IQ', 'IR', 'JO', 'KW', 'LY', 'OM', 'QA', 'SD', 'SY',
]);

type WeekInfoLocale = Intl.Locale & { getWeekInfo?: () => { firstDay: number } };

export function firstDayOfWeek(locale: string): number {
    let region: string | undefined;

    try {
        const resolved = new Intl.Locale(locale) as WeekInfoLocale;

        // took firefox until JULY TWENTY TWENTY SIX BTW
        const info = resolved.getWeekInfo?.();
        if (info !== undefined) return info.firstDay % 7;

        region = resolved.region ?? undefined;
    } catch {
        region = undefined;
    }

    const code = (region ?? locale.split('-')[1] ?? '').toUpperCase();

    if (SUNDAY_REGIONS.has(code)) return 0;
    if (SATURDAY_REGIONS.has(code)) return 6;

    return 1;
}
