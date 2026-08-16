import assert from 'node:assert/strict';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, relative, resolve } from 'node:path';
import { describe, it } from 'node:test';

const srcRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..', 'src');

function sources(dir: string): string[] {
    return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
        const full = join(dir, entry.name);
        if (entry.isDirectory()) return sources(full);
        return entry.isFile() && entry.name.endsWith('.ts') ? [full] : [];
    });
}

// prose mentions browser stuff all the time, so this only looks at code
function withoutComments(text: string): string {
    return text.replace(/\/\*[\s\S]*?\*\//g, ' ').replace(/\/\/[^\n]*/g, ' ');
}

const files = sources(srcRoot).map((file) => ({
    path: relative(srcRoot, file).replaceAll('\\', '/'),
    text: readFileSync(file, 'utf8'),
    code: withoutComments(readFileSync(file, 'utf8')),
    imports: [...readFileSync(file, 'utf8').matchAll(/from '([^']*)'/g)].map((m) => {
        const target = m[1]!;
        if (!target.startsWith('.')) return target;
        return relative(srcRoot, resolve(dirname(file), target)).replaceAll('\\', '/')
            .replace(/\.ts$/, '');
    }),
}));

it('finds the sources it is supposed to check', () => {
    assert.ok(files.length > 4, `only found ${files.length} source files`);
    assert.ok(files.some((f) => f.path.startsWith('memory/')));
});

describe('the import rules that keep the layers acyclic', () => {
    it('model.ts imports nothing', () => {
        const model = files.find((f) => f.path === 'model.ts');
        assert.deepEqual(model?.imports, []);
    });

    it('store.ts and search.ts reach no further than the model', () => {
        for (const file of files) {
            if (file.path !== 'store.ts' && file.path !== 'search.ts') continue;

            for (const target of file.imports)
                assert.equal(target, 'model', `${file.path} imports ${target}`);
        }
    });

    it('a backend imports the contract, never the other backend', () => {
        const allowed = new Set(['model', 'store', 'search']);

        for (const file of files) {
            const backend = file.path.startsWith('memory/') ? 'memory'
                : file.path.startsWith('idb/') ? 'idb'
                    : undefined;
            if (backend === undefined) continue;

            for (const target of file.imports) {
                if (target.startsWith(`${backend}/`)) continue;
                assert.ok(allowed.has(target),
                    `${file.path} imports ${target}; a backend may only reach the contract`);
            }
        }
    });
});

describe('the store knows nothing about the protocol', () => {
    it('never imports eas-client', () => {
        for (const file of files)
            for (const target of file.imports)
                assert.ok(!target.startsWith('@relivewp/eas-client'),
                    `${file.path} imports ${target}; the store must not know the wire format`);
    });
});

describe('only the IndexedDB backend touches the browser', () => {
    const globals = /\b(indexedDB|IDBKeyRange|IDBDatabase|IDBRequest|IDBTransaction|window|document|navigator)\b/;

    it('no other source names a DOM global', () => {
        for (const file of files) {
            if (file.path.startsWith('idb/') || file.path === 'idb.ts') continue;

            const found = globals.exec(file.code);
            assert.equal(found, null, `${file.path} names ${found?.[0]} outside idb/`);
        }
    });
});
