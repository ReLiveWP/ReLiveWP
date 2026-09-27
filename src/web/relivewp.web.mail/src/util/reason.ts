export function reason(thrown: unknown): string {
    return thrown instanceof Error ? thrown.message : String(thrown);
}
