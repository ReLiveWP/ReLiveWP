import { EAS_STATUS, type EasStatusTableName } from './generated/status.g.ts';

// not universal, MoveItems succeeds with 3 and ping uses both 1 and 2, so those check themselves
export const SUCCESS = 1;

// any command can come back with a common code on top of its own
export function isDocumentedStatus(name: EasStatusTableName, value: number): boolean {
    return EAS_STATUS[name].values.includes(value) || EAS_STATUS.Common.values.includes(value);
}

export function statusCitation(name: EasStatusTableName): string {
    const { spec, section } = EAS_STATUS[name];
    return section === '' ? spec : `${spec} ${section}`;
}

export type { EasStatusTableName };
