import type { Contact } from "@relivewp/eas-store";
import { useCallback, useEffect, useState } from "preact/hooks";

import {
    pairKey, withLinked, withoutMember, type Aggregate, type AggregateOverrides, type OverrideKind,
} from "./aggregate";

// browser local for now, nothing about this reaches the server or a phone
const EMPTY: AggregateOverrides = new Map();

const STORAGE_PREFIX = "relivewp.mail.contactlinks.";

type Stored = { a: string, b: string, kind: OverrideKind };

function storageKey(userId: string): string {
    return STORAGE_PREFIX + userId;
}

export function readOverrides(userId: string): AggregateOverrides {
    try {
        const text = localStorage.getItem(storageKey(userId));
        if (text === null) return EMPTY;

        const parsed = JSON.parse(text) as Stored[];
        const map = new Map<string, OverrideKind>();
        for (const entry of parsed)
            if (entry.kind === "merge" || entry.kind === "split") map.set(pairKey(entry.a, entry.b), entry.kind);

        return map;
    } catch {
        return EMPTY;
    }
}

export function writeOverrides(userId: string, overrides: AggregateOverrides): void {
    const entries: Stored[] = [];
    for (const [key, kind] of overrides) {
        const [a, b] = key.split("|") as [string, string];
        entries.push({ a, b, kind });
    }

    try {
        if (entries.length === 0) localStorage.removeItem(storageKey(userId));
        else localStorage.setItem(storageKey(userId), JSON.stringify(entries));
    } catch {
        // private windows and full quotas, the in-memory copy still applies this session
    }
}

export function useAggregateOverrides(userId: string | null): {
    overrides: AggregateOverrides,
    unlink: (member: Contact, others: Contact[]) => void,
    link: (target: Aggregate, other: Aggregate) => void,
} {
    const [overrides, setOverrides] = useState<AggregateOverrides>(EMPTY);

    useEffect(() => {
        setOverrides(userId === null ? EMPTY : readOverrides(userId));
    }, [userId]);

    const update = useCallback((next: (current: AggregateOverrides) => AggregateOverrides) => {
        setOverrides((current) => {
            const updated = next(current);
            if (userId !== null) writeOverrides(userId, updated);
            return updated;
        });
    }, [userId]);

    const unlink = useCallback((member: Contact, others: Contact[]) => {
        update((current) => withoutMember(current, member, others));
    }, [update]);

    const link = useCallback((target: Aggregate, other: Aggregate) => {
        update((current) => withLinked(current, target, other));
    }, [update]);

    return { overrides, unlink, link };
}
