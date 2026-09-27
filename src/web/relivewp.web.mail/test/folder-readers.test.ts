import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { dirname, join, relative, resolve } from 'node:path';
import { describe, it } from 'node:test';
import { fileURLToPath } from 'node:url';

const srcRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..', 'src');

// reads scoped to a folder, so a change to that folder makes what they returned stale
const FOLDER_READS = [
    'listContacts',
    'listFavourites',
    'meContact',
    'contactPhotos',
    'listMessages',
    'listEvents',
    'listTasks',
];

function sources(dir: string): string[] {
    return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
        const full = join(dir, entry.name);
        if (entry.isDirectory()) return sources(full);
        return entry.isFile() && /\.tsx?$/.test(entry.name) ? [full] : [];
    });
}

function withoutComments(text: string): string {
    return text.replace(/\/\*[\s\S]*?\*\//g, ' ').replace(/\/\/[^\n]*/g, ' ');
}

const files = sources(srcRoot).map((file) => ({
    path: relative(srcRoot, file).replaceAll('\\', '/'),
    code: withoutComments(readFileSync(file, 'utf8')),
}));

const readers = files.filter((file) => FOLDER_READS.some((read) => file.code.includes(`client.${read}(`)));

describe('every folder-scoped reader re-reads when that folder changes', () => {
    it('finds the readers it is supposed to check', () => {
        assert.ok(files.length > 10, `only found ${files.length} source files`);
        assert.ok(readers.length >= 4, `only found ${readers.length} folder-scoped readers`);
    });

    // contact photos were read once and never again, so a picture synced from the phone did not
    // appear until the page was reloaded. Subscribing is not optional for a folder-scoped read.
    for (const reader of readers) {
        it(`${reader.path} subscribes via useFolderChanges`, () => {
            assert.ok(
                reader.code.includes('useFolderChanges('),
                `${reader.path} reads a folder but never subscribes to changes on it`);
        });
    }
});
