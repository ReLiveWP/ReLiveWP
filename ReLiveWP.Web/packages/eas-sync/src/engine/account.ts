import type { EasSession } from '@relivewp/eas-client';
import type { EasStore } from '@relivewp/eas-store';

import { readPolicy } from './policy.ts';
import type { Recovery } from './status.ts';

export interface AccountOptions {
    now: () => number;
    force?: boolean | undefined;
}

export interface ProvisionOutcome {
    recovery: Recovery;
    policyKey: number | null;
    wiped: boolean;
    provisioned: boolean;
}

export async function ensureProvisioned(
    session: EasSession, store: EasStore, options: AccountOptions): Promise<ProvisionOutcome> {
    const account = await store.account();

    if (options.force !== true
        && session.policyKey !== undefined
        && account.policyKey === session.policyKey)
        return {
            recovery: { kind: 'ok' },
            policyKey: account.policyKey,
            wiped: false,
            provisioned: false,
        };

    const result = await session.provision();

    // destroy first, acknowledge after, so a server that never answers still deletes the mailbox.
    if (result.remoteWipe || result.accountOnlyRemoteWipe) {
        await store.wipe();
        await session
            .acknowledgeRemoteWipe(1, result.accountOnlyRemoteWipe && !result.remoteWipe)
            .catch(() => undefined);

        return { recovery: { kind: 'wipe' }, policyKey: null, wiped: true, provisioned: false };
    }

    if (result.policyKey === undefined)
        return {
            recovery: {
                kind: 'fail',
                reason: `provisioning returned status ${result.ackStatus ?? result.downloadStatus}`,
            },
            policyKey: account.policyKey,
            wiped: false,
            provisioned: false,
        };

    await store.saveAccount({
        policyKey: result.policyKey,
        provisionedAt: options.now(),
        policy: readPolicy(result.provisionDoc),
    });

    return {
        recovery: { kind: 'ok' },
        policyKey: result.policyKey,
        wiped: false,
        provisioned: true,
    };
}
