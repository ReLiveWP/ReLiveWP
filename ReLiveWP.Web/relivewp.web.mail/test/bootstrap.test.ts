import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { bootstrapUser } from '../src/util/bootstrap.ts';

const URL = 'https://login.example/auth/user/@me';

const USER = {
    id: 'user-1',
    cid: 'cid-1',
    puid: 'puid-1',
    username: 'someone',
    email_address: 'someone@example.com',
};

function responding(status: number, body?: unknown): typeof fetch {
    return async () => new Response(
        body === undefined ? null : JSON.stringify(body),
        { status, headers: { 'Content-Type': 'application/json' } });
}

function throwing(): typeof fetch {
    return async () => { throw new TypeError('Failed to fetch'); };
}

describe('bootstrapUser', () => {
    it('returns the user when the service answers', async () => {
        const outcome = await bootstrapUser(responding(200, USER), URL);

        assert.deepEqual(outcome, { kind: 'ok', user: USER });
    });

    it('reports a refusal for 401 and 403', async () => {
        for (const status of [401, 403])
            assert.deepEqual(await bootstrapUser(responding(status), URL), { kind: 'rejected' },
                `status ${status} should invalidate the session`);
    });

    it('reports unreachable when the request throws', async () => {
        assert.deepEqual(await bootstrapUser(throwing(), URL), { kind: 'unreachable' });
    });

    it('reports unreachable for server faults rather than signing out', async () => {
        for (const status of [500, 502, 503, 504])
            assert.deepEqual(await bootstrapUser(responding(status), URL), { kind: 'unreachable' },
                `status ${status} should leave the session alone`);
    });

    it('reports unreachable when a 200 body is not json', async () => {
        const broken: typeof fetch = async () => new Response('<html>not json</html>', { status: 200 });

        assert.deepEqual(await bootstrapUser(broken, URL), { kind: 'unreachable' });
    });
});
