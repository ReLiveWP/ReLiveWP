// reads the spec docs and writes spec/*.json, which is what generate.mjs consumes. only needed
// when a spec revision changes, a normal build never touches the docs.
//
//   node tools/extract.mjs --docs <dir>      write spec/*.json
//   node tools/extract.mjs --docs <dir> --check
//
// --docs can also come from EAS_SPEC_DOCS. no default on purpose, a path that only works on one
// machine shouldn't be committed.

import { readFileSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const pkgRoot = resolve(here, '..');
const SPEC_DIR = resolve(pkgRoot, 'spec');
const EXTENSIONS = resolve(SPEC_DIR, 'extensions.json');

const args = process.argv.slice(2);
const flag = (name) => args.includes(name);
const opt = (name) => {
    const i = args.indexOf(name);
    return i === -1 || i + 1 >= args.length ? undefined : args[i + 1];
};

function fail(message) {
    console.error(`extract: ${message}`);
    process.exit(2);
}

const docsDir = opt('--docs') ?? process.env['EAS_SPEC_DOCS'];
if (docsDir === undefined)
    fail('pass --docs <dir> or set EAS_SPEC_DOCS to the directory holding the MS-AS* markdown.\n'
        + '        Only needed to refresh spec/*.json. A normal build does not require it.');

function read(name) {
    try {
        return readFileSync(resolve(docsDir, `${name}.md`), 'utf8');
    } catch (e) {
        return fail(`cannot read ${name}.md in ${docsDir}: ${e.message}`);
    }
}

// last row of the Revision Summary table is the revision the doc is at
function revisionOf(text, name) {
    const start = text.indexOf('**Revision Summary**');
    if (start === -1) return name;

    const table = text.slice(start, text.indexOf('\n# ', start));
    const rows = [...table.matchAll(/^\s{2,}(\d{1,2}\/\d{1,2}\/\d{4})\s+(\d+\.\d+(?:\.\d+)?)\s/gm)];
    const last = rows.at(-1);

    return last === undefined ? name : `${name} ${last[2]} (${last[1]})`;
}

const xmlnsFor = (ns) => ns.toLowerCase();

function codePages(text) {
    const pages = [];
    let current = null;

    for (const line of text.split(/\r?\n/)) {
        const heading = /^#{2,6}\s+Code Page (\d+):\s*(\S+)\s*$/.exec(line);
        if (heading) {
            current = { page: Number(heading[1]), ns: heading[2], xmlns: xmlnsFor(heading[2]), tokens: {}, versions: {} };
            pages.push(current);
            continue;
        }
        if (!current) continue;

        // rows often carry a footnote marker between the name and the token:
        //   **Body** --- see note 1 following this table   0x09   2.5
        const row = /^\s*\*\*([A-Za-z0-9_]+)\*\*.*?\s(0x[0-9A-Fa-f]{1,2})\s+(\S.*?)\s*$/.exec(line);
        if (!row) continue;

        const [, tag, tokenHex, versions] = row;
        const token = Number.parseInt(tokenHex, 16);

        if (token < 0x05 || token > 0x3f)
            fail(`code page ${current.page}: ${tag} has token ${tokenHex}, outside the 0x05-0x3F tag range`);
        if (current.tokens[tag] !== undefined) fail(`code page ${current.page}: duplicate tag ${tag}`);
        if (Object.values(current.tokens).includes(token)) fail(`code page ${current.page}: token ${tokenHex} twice`);

        current.tokens[tag] = token;
        current.versions[tag] = versions === 'All' ? 'All' : versions.split(/,\s*/);
    }

    if (pages.length === 0) fail('no code pages found; has the document format changed?');
    return pages;
}

const clean = (text) => text
    .replace(/\[([^\]]*)\]\([^)]*\)/g, '$1')
    .replace(/\*\*/g, '')
    .replace(/\\(.)/g, '$1')
    .replace(/\s+/g, ' ')
    .trim();

const isGridRule = (line) => /^\s*\+[-=+]+\+\s*$/.test(line);
const isSpanRule = (line) => /^\s*-{2,}(\s+-{2,})+\s*$/.test(line);
const isPlainRule = (line) => /^\s*-{4,}\s*$/.test(line);

function parseGrid(lines, from) {
    const rows = [];
    let current = null;
    let i = from;

    for (; i < lines.length; i++) {
        const line = lines[i];
        if (isGridRule(line)) {
            if (current) { rows.push(current); current = null; }
            if (rows.length > 0 && !lines[i + 1]?.trimStart().startsWith('|')) break;
            continue;
        }
        if (!line.trimStart().startsWith('|')) break;

        const cells = line.split('|').slice(1, -1).map((c) => c.trim());
        if (current === null) current = cells;
        else for (let c = 0; c < cells.length; c++) if (cells[c]) current[c] = `${current[c] ?? ''} ${cells[c]}`;
    }
    if (current) rows.push(current);

    return { rows, next: i };
}

function parseSpans(lines, ruleAt) {
    const spans = [];
    for (const m of lines[ruleAt].matchAll(/-{2,}/g)) spans.push([m.index, m.index + m[0].length]);

    const cut = (line, at) => {
        const [start, end] = spans[at];
        return (at === spans.length - 1 ? line.slice(start) : line.slice(start, end)).trim();
    };

    const rows = [Array.from(spans, (_, c) => cut(lines[ruleAt - 1], c))];
    let current = null;

    let i = ruleAt + 1;
    for (; i < lines.length; i++) {
        const line = lines[i];
        if (isPlainRule(line) || isSpanRule(line)) break;
        if (line.trim() === '') continue;

        const cells = Array.from(spans, (_, c) => cut(line, c));
        if (cells[0] !== '') { if (current) rows.push(current); current = cells; }
        else if (current) for (let c = 0; c < cells.length; c++) if (cells[c]) current[c] = `${current[c]} ${cells[c]}`;
    }
    if (current) rows.push(current);

    return { rows, next: i };
}

// a section can hold several status tables, Search has three. merged with first meaning winning,
// since callers only ask whether a value is documented for the command.
function statusesIn(lines) {
    const merged = new Map();

    for (let i = 0; i < lines.length; i++) {
        let parsed;
        if (isGridRule(lines[i]) && lines[i + 1]?.trimStart().startsWith('|')) parsed = parseGrid(lines, i);
        else if (isSpanRule(lines[i]) && i > 0) parsed = parseSpans(lines, i);
        else continue;

        i = Math.max(i, parsed.next - 1);

        const header = (parsed.rows[0] ?? []).map((c) => clean(c).toLowerCase());
        const meaningAt = header.findIndex((h) => h.startsWith('meaning'));
        if (!/^(value|code|status code)$/.test(header[0] ?? '') || meaningAt < 1) continue;

        const nameAt = header.findIndex((h) => h.startsWith('element name'));
        const scopeAt = header.findIndex((h) => h.startsWith('scope'));

        for (const row of parsed.rows.slice(1)) {
            const value = /^-?\d+/.exec(clean(row[0] ?? ''));
            if (!value || merged.has(Number(value[0]))) continue;

            const name = nameAt === -1 ? '' : clean(row[nameAt] ?? '');
            const scope = scopeAt === -1 ? '' : clean(row[scopeAt] ?? '');

            merged.set(Number(value[0]), {
                value: Number(value[0]),
                meaning: clean(row[meaningAt] ?? ''),
                ...(name === '' ? {} : { name }),
                ...(scope === '' ? {} : { scope }),
            });
        }
    }

    return merged.size === 0 ? undefined : [...merged.values()].sort((a, b) => a.value - b.value);
}

const COMMON = 'Common';

function statusTables(text, spec) {
    const lines = text.split(/\r?\n/);
    const toc = new Map();
    for (const line of lines) {
        const m = /^\[([0-9.]+)\s+(Common Status Codes|Status \(([A-Za-z]+)\))\s/.exec(line);
        if (m && !toc.has(m[3] ?? COMMON)) toc.set(m[3] ?? COMMON, m[1]);
    }

    const found = [];
    for (let i = 0; i < lines.length; i++) {
        const heading = /^(#{2,6})\s+(Common Status Codes|Status \(([A-Za-z]+)\))\s*$/.exec(lines[i]);
        if (!heading) continue;

        const depth = heading[1].length;
        let end = i + 1;
        while (end < lines.length && !new RegExp(`^#{2,${depth}}\\s`).test(lines[end])) end++;

        const key = heading[3] ?? COMMON;
        const statuses = statusesIn(lines.slice(i + 1, end));
        if (statuses) found.push({ key, spec, section: toc.get(key) ?? '', statuses });
    }

    return found;
}

const SCALARS = {
    'xs:string': 'string',
    'xs:boolean': 'boolean',
    'xs:integer': 'number',
    'xs:int': 'number',
    'xs:unsignedByte': 'number',
    'xs:unsignedShort': 'number',
    'xs:unsignedInt': 'number',
    'xs:short': 'number',
    'xs:long': 'number',
    'xs:dateTime': 'Date',
    'xs:base64Binary': 'Uint8Array',
};

// the docs come out backslash-escaped, with list prefixes glued on and wrapped mid-element, so
// the schema has to be put back together first
function schemasIn(text) {
    const start = text.indexOf('# Appendix A: Full XML Schema');
    if (start === -1) return [];

    const body = text.slice(start).replace(/\\(.)/g, '$1').replace(/^\s*\d+\.\s+(?=<)/gm, '');
    const found = [];

    for (const m of body.matchAll(/<xs:schema\b[\s\S]*?<\/xs:schema>/g)) {
        const xml = m[0].replace(/\s+/g, ' ');
        const ns = /targetNamespace="([^"]+)"/.exec(xml);
        if (ns) found.push({ ns: ns[1], xml });
    }

    return found;
}

// only direct children of xs:schema are declarations, the stack keeps nested complexTypes out
function declarationsIn(xml) {
    const stack = [];
    const found = new Map();
    let current;

    for (const tag of xml.match(/<\/?[^>]+>/g) ?? []) {
        if (tag.startsWith('<?') || tag.startsWith('<!')) continue;

        const name = /<\/?\s*([\w:]+)/.exec(tag)?.[1];
        if (!name) continue;

        if (tag.startsWith('</')) {
            const left = stack.pop();
            if (current && left === 'xs:element' && stack.length === 1) {
                found.set(current.name, current);
                current = undefined;
            }
            continue;
        }

        const depth = stack.length;
        const selfClosing = tag.endsWith('/>');
        if (!selfClosing) stack.push(name);

        if (name !== 'xs:element') {
            if (current && current.type === undefined && name === 'xs:restriction')
                current.type = SCALARS[/base="([^"]+)"/.exec(tag)?.[1] ?? ''];
            if (current && name === 'xs:complexType') current.complex = true;
            continue;
        }
        if (depth !== 1) continue;

        const elementName = /name="([^"]+)"/.exec(tag)?.[1];
        if (!elementName) continue;

        const declared = /\btype="([^"]+)"/.exec(tag)?.[1];
        const entry = { name: elementName, type: declared === undefined ? undefined : SCALARS[declared] };

        if (selfClosing) found.set(entry.name, entry);
        else current = entry;
    }

    return found;
}

// AllProps is what an item's ApplicationData can carry. the standalone declarations are wider,
// they include nested-only members like AttName.
function allPropsIn(xml) {
    const group = /<xs:group name="AllProps">([\s\S]*?)<\/xs:group>/.exec(xml);
    if (!group) return undefined;

    return [...group[1].matchAll(/<xs:element\s+(?:ref|name)="([^"]+)"/g)]
        .map((m) => m[1].replace(/^[a-z0-9]+:/i, ''));
}

function itemNamespaces(text, wanted) {
    const schemas = schemasIn(text);
    const out = {};

    for (const ns of wanted) {
        const schema = schemas.find((s) => s.ns === ns);
        if (!schema) fail(`no schema for namespace ${ns}`);

        const declared = declarationsIn(schema.xml);
        // notes has no AllProps, every element it declares is an item property
        const props = allPropsIn(schema.xml) ?? [...declared.keys()];

        out[ns] = props.map((name) => {
            const entry = declared.get(name);
            return { name, ts: entry === undefined || entry.complex || entry.type === undefined ? 'node' : entry.type };
        });
    }

    return out;
}

const ITEM_DOCS = [
    ['MS-ASEMAIL', ['Email', 'Email2']],
    ['MS-ASAIRS', ['AirSyncBase']],
    ['MS-ASCAL', ['Calendar']],
    ['MS-ASCNTC', ['Contacts', 'Contacts2']],
    ['MS-ASTASK', ['Tasks']],
    ['MS-ASNOTE', ['Notes']],
];

const banner = (sources) => ({
    $comment: 'GENERATED by tools/extract.mjs. Committed on purpose: it is what lets the package build '
        + 'without the specification documents. Edit the extractor, not this file.',
    sources,
});

const wbxml = read('MS-ASWBXML');
const extensions = JSON.parse(readFileSync(EXTENSIONS, 'utf8'));

const pages = codePages(wbxml);
for (const page of extensions.pages) {
    if (pages.some((p) => p.page === page.page)) fail(`extensions.json redefines spec code page ${page.page}`);
    pages.push({ page: page.page, ns: page.ns, xmlns: page.xmlns, tokens: page.tokens, versions: {} });
}
pages.sort((a, b) => a.page - b.page);

const ascmd = read('MS-ASCMD');
const asprov = read('MS-ASPROV');
const tables = {};
for (const entry of [...statusTables(ascmd, 'MS-ASCMD'),
                     ...statusTables(asprov, 'MS-ASPROV').filter((t) => t.key !== COMMON)])
    if (!(entry.key in tables))
        tables[entry.key] = { spec: entry.spec, section: entry.section, statuses: entry.statuses };

for (const required of [COMMON, 'Sync', 'Provision'])
    if (!(required in tables)) fail(`no status table found for ${required}`);

const namespaces = {};
const itemSources = [];
for (const [doc, wanted] of ITEM_DOCS) {
    const text = read(doc);
    Object.assign(namespaces, itemNamespaces(text, wanted));
    itemSources.push(revisionOf(text, doc));
}

const outputs = [
    ['codepages.json', {
        ...banner([revisionOf(wbxml, 'MS-ASWBXML'), 'spec/extensions.json']),
        pageCount: pages.length,
        tokenCount: pages.reduce((n, p) => n + Object.keys(p.tokens).length, 0),
        pages,
    }],
    ['status.json', {
        ...banner([revisionOf(ascmd, 'MS-ASCMD'), revisionOf(asprov, 'MS-ASPROV')]),
        tables,
    }],
    ['items.json', { ...banner(itemSources), namespaces }],
];

const rendered = outputs.map(([name, body]) => [resolve(SPEC_DIR, name), `${JSON.stringify(body, null, 2)}\n`]);

if (flag('--check')) {
    const stale = rendered.filter(([file, content]) => {
        try { return readFileSync(file, 'utf8') !== content; } catch { return true; }
    });
    if (stale.length) {
        console.error(`extract: spec/ is out of date, run \`pnpm extract\`:\n${stale.map(([f]) => `  ${f}`).join('\n')}`);
        process.exit(1);
    }
    console.log('extract: spec/ is up to date');
    process.exit(0);
}

for (const [file, content] of rendered) writeFileSync(file, content);
console.log(`extract: ${pages.length} code pages, ${Object.keys(tables).length} status tables, `
    + `${Object.keys(namespaces).length} item namespaces`);
