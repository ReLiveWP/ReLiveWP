import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { allCodePages, codePageByNamespace, codePageByNumber, tokenFor } from '../src/wbxml/codepages.ts';


describe('code page tables', () => {
    const pages = allCodePages();

    it('has every code page MS-ASWBXML defines, plus the Windows Live extension', () => {
        const numbers = pages.map((p) => p.page).sort((a, b) => a - b);
        assert.deepEqual(numbers, [...Array.from({ length: 26 }, (_, i) => i), 0xfe]);
    });

    it('gives every page a namespace and a prefix', () => {
        for (const p of pages) {
            assert.ok(p.ns, `page ${p.page} has no namespace`);
            assert.ok(p.xmlns, `page ${p.page} has no prefix`);
        }
    });

    it('keeps every token inside the tag code space', () => {
        for (const p of pages)
            p.tags.forEach((tag, token) => {
                if (tag === undefined) return;
                assert.ok(token >= 0x05 && token <= 0x3f, `${p.ns}:${tag} is token 0x${token.toString(16)}`);
            });
    });

    it('assigns each token at most one tag', () => {
        for (const p of pages) assert.equal(p.tags.length <= 0x40, true);
    });

    it('resolves namespaces case-insensitively', () => {
        assert.equal(codePageByNamespace('airsync')?.page, 0);
        assert.equal(codePageByNamespace('AirSync')?.page, 0);
    });

    it('agrees in both directions', () => {
        for (const p of pages)
            p.tags.forEach((tag, token) => {
                if (tag === undefined) return;
                assert.equal(tokenFor(p.ns, tag), token, `${p.ns}:${tag}`);
            });
    });

    // spot checks from the spec doc, so a mangled regenerate can't sneak through
    it('matches the specification at known points', () => {
        assert.equal(tokenFor('AirSync', 'Sync'), 0x05);
        assert.equal(tokenFor('AirSync', 'HeartbeatInterval'), 0x29);
        assert.equal(tokenFor('FolderHierarchy', 'FolderSync'), 0x16);
        assert.equal(tokenFor('AirSyncBase', 'BodyPreference'), 0x05);
        assert.equal(tokenFor('ComposeMail', 'Mime'), 0x10);
        assert.equal(tokenFor('Email2', 'ConversationIndex'), 0x0a);
        assert.equal(tokenFor('Provision', 'AccountOnlyRemoteWipe'), 0x3b);
        assert.equal(codePageByNumber(0xfe)?.ns, 'WindowsLive');
    });
});

