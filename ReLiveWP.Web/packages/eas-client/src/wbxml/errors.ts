export class WbxmlError extends Error {
    readonly offset: number | undefined;

    constructor(message: string, offset?: number) {
        super(offset === undefined ? message : `${message} (at byte ${offset})`);
        this.name = 'WbxmlError';
        this.offset = offset;
    }
}
