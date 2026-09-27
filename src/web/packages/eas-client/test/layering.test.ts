import assert from 'node:assert/strict';
import { execFileSync } from 'node:child_process';
import { readFileSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join, relative, resolve } from 'node:path';
import { describe, it, test } from 'node:test';

const pkgRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const srcRoot = resolve(pkgRoot, 'src');

function sources(dir: string): string[] {
    return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
        const full = join(dir, entry.name);
        if (entry.isDirectory()) return sources(full);
        return entry.isFile() && entry.name.endsWith('.ts') ? [full] : [];
    });
}

function localImports(file: string): string[] {
    return [...readFileSync(file, 'utf8').matchAll(/from '(\.[^']*)'/g)]
        .map((m) => relative(srcRoot, resolve(dirname(file), m[1]!)).replaceAll('\\', '/'));
}

const files = sources(srcRoot).map((file) => ({
    path: relative(srcRoot, file).replaceAll('\\', '/'),
    imports: localImports(file),
}));

it('finds the sources it is supposed to check', () => {
    assert.ok(files.length > 15, `only found ${files.length} source files`);
    assert.ok(files.some((f) => f.path.startsWith('nodes/')));
    assert.ok(files.some((f) => f.path.startsWith('generated/')));
});

describe('the import rules that keep the layers acyclic', () => {
    it('nodes/ imports nothing else in the package', () => {
        for (const file of files) {
            if (!file.path.startsWith('nodes/')) continue;

            for (const target of file.imports)
                assert.ok(target.startsWith('nodes/'),
                    `${file.path} imports ${target}; nodes/ must not depend on anything above it`);
        }
    });

    it('generated/ imports only nodes/ and other generated/', () => {
        for (const file of files) {
            if (!file.path.startsWith('generated/')) continue;

            for (const target of file.imports)
                assert.ok(target.startsWith('nodes/') || target.startsWith('generated/'),
                    `${file.path} imports ${target}; generated code may only reach nodes/`);
        }
    });
});

describe('generated code is quarantined', () => {
    it('lives only under generated/', () => {
        for (const file of files)
            if (file.path.endsWith('.g.ts'))
                assert.ok(file.path.startsWith('generated/'), `${file.path} is generated but sits outside generated/`);
    });

    it('is the only thing under generated/', () => {
        for (const file of files)
            if (file.path.startsWith('generated/'))
                assert.ok(file.path.endsWith('.g.ts'), `${file.path} is hand-authored but sits under generated/`);
    });
});

describe('the wbxml codec stays self-contained', () => {
    const internals = ['wbxml/reader.ts', 'wbxml/writer.ts', 'wbxml/codepages.ts', 'wbxml/tokens.ts'];

    it('nothing outside wbxml/ reaches into its internals', () => {
        for (const file of files) {
            if (file.path.startsWith('wbxml/')) continue;

            for (const target of file.imports)
                assert.ok(!internals.includes(target),
                    `${file.path} imports ${target}; that is a wbxml internal, go through encode/decode`);
        }
    });
});

test('src/generated is current with spec/', () => {
    execFileSync(process.execPath, ['tools/generate.mjs', '--check'], { cwd: pkgRoot, stdio: 'pipe' });
});
