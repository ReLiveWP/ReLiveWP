import { EAS_STATUS_TEXT, type EasStatusText } from './generated/status-text.g.ts';
import type { EasStatusTableName } from './generated/status.g.ts';

export function statusText(name: EasStatusTableName, value: number): EasStatusText | undefined {
    return EAS_STATUS_TEXT[name][String(value)] ?? EAS_STATUS_TEXT.Common[String(value)];
}

export function describeStatus(name: EasStatusTableName, value: number | undefined): string {
    if (value === undefined) return 'no Status';

    const text = statusText(name, value);
    return text === undefined ? `Status ${value} (undocumented)` : `Status ${value} (${text.meaning})`;
}

export type { EasStatusText };
