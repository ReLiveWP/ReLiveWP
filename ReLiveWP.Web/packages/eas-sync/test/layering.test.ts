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

// prose mentions browser stuff all the time, so this only looks at code. quoted strings go the
// same way as comments, because a status message naming the document library is prose too.
// template literals stay, they can hold real code between the braces
function withoutProse(text: string): string {
    return text
        .replace(/\/\*[\s\S]*?\*\//g, ' ')
        .replace(/\/\/[^\n]*/g, ' ')
        .replace(/'(?:[^'\\\n]|\\.)*'/g, "''")
        .replace(/"(?:[^"\\\n]|\\.)*"/g, '""');
}

const files = sources(srcRoot).map((file) => {
    const text = readFileSync(file, 'utf8');
    return {
        path: relative(srcRoot, file).replaceAll('\\', '/'),
        text,
        code: withoutProse(text),
        imports: [...text.matchAll(/from '([^']*)'/g)].map((match) => {
            const target = match[1]!;
            if (!target.startsWith('.')) return target;
            return relative(srcRoot, resolve(dirname(file), target))
                .replaceAll('\\', '/')
                .replace(/\.ts$/, '');
        }),
    };
});

const layerOf = (path: string): string | undefined => path.split('/')[0];

it('finds the sources it is supposed to check', () => {
    assert.ok(files.length > 5, `only found ${files.length} source files`);
    assert.ok(files.some((file) => file.path.startsWith('project/')));
    assert.ok(files.some((file) => file.path.startsWith('engine/')));
});

describe('the import rules that keep the layers acyclic', () => {
    it('project/ knows both vocabularies and nothing else in the package', () => {
        for (const file of files) {
            if (!file.path.startsWith('project/')) continue;

            for (const target of file.imports)
                assert.ok(target.startsWith('project/') || target.startsWith('@relivewp/'),
                    `${file.path} imports ${target}; project/ translates, it does not orchestrate`);
        }
    });

    it('engine/ never reaches up into the scheduler or the host', () => {
        for (const file of files) {
            if (!file.path.startsWith('engine/')) continue;

            for (const target of file.imports) {
                const layer = target.startsWith('@relivewp/') ? undefined : layerOf(target);
                assert.ok(layer === undefined || layer === 'engine' || layer === 'project',
                    `${file.path} imports ${target}; the engine must not know where it runs`);
            }
        }
    });
});

describe('the engine can run under node', () => {
    const globals = /\b(window|document|navigator|indexedDB|BroadcastChannel|postMessage)\b/;

    it('no layer below host/ names a browser global', () => {
        for (const file of files) {
            if (file.path.startsWith('host/') || file.path === 'host.ts' || file.path === 'worker.ts')
                continue;

            const found = globals.exec(file.code);
            assert.equal(found, null, `${file.path} names ${found?.[0]} outside host/`);
        }
    });
});
