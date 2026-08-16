import "./message-list.scss";

import type { EasClient } from "@relivewp/eas-sync/host";
import type { Message } from "@relivewp/eas-store";
import { defaultRangeExtractor, type Range } from "@tanstack/virtual-core";
import { useCallback, useEffect, useMemo, useRef } from "preact/hooks";

import { useVirtualizer } from "~/hooks/useVirtualizer";
import { recall } from "../opened";
import { stamp, toRows, type Row } from "./groups";
import { useMessages } from "./useMessages";
import { useMessageSearch } from "./useMessageSearch";

const ROW_HEIGHT = 73;
const HEADER_HEIGHT = 30;
const OVERSCAN = 8;

type Props = {
    client: EasClient | null,
    folderId: string,
    query: string,
    selected: string | null,
    onSelect: (message: Message) => void,
    onFallback: (messageId: string) => void,
};

type CanvasProps = {
    rows: Row[],
    now: number,
    exhausted: boolean,
    loading: boolean,
    selected: string | null,
    onSelect: (message: Message) => void,
    onLoadMore: () => void,
};

function activeHeader(headers: number[], startIndex: number): number | null {
    let found: number | null = null;

    for (const index of headers) {
        if (index > startIndex) break;
        found = index;
    }

    return found;
}

function MessageRow({ message, now }: { message: Message, now: number }) {
    const who = message.from?.name ?? message.from?.email ?? "unknown sender";

    return (
        <>
            <span class="message-from">{who}</span>
            <span class="message-stamp">{stamp(message.receivedAt, now)}</span>
            <span class="message-subject">{message.subject.length > 0 ? message.subject : "(no subject)"}</span>
            <span class="message-preview">{message.preview}</span>
        </>
    );
}

export default function MessageList(
    { client, folderId, query, selected, onSelect, onFallback }: Props,
) {
    const { messages, loading, error, exhausted, loadMore } = useMessages(client, folderId);
    const { results, searching, error: searchError } = useMessageSearch(client, folderId, query);

    const listed = results ?? messages;

    const { rows, now } = useMemo(() => {
        const now = Date.now();
        return { rows: toRows(listed, now), now };
    }, [listed]);

    // the one redirect: nothing is open and there is now something to open. gated on the list
    // actually holding messages, so it fires once per folder rather than churning against an
    // empty list, and it never fires again because the url stops being empty
    useEffect(() => {
        if (selected !== null || listed.length === 0) return;

        const remembered = recall(folderId);
        const restored = remembered !== null
            && listed.some((message) => message.id === remembered);

        onFallback(restored ? remembered : listed[0]!.id);
    }, [listed, selected, folderId, onFallback]);

    const problem = error ?? searchError;
    const busy = loading || searching;

    if (problem !== null) return <p class="error">{problem}</p>;

    if (rows.length === 0)
        return busy
            ? <p class="note">Loading.</p>
            : <p class="note">{results === null ? "No messages." : "Nothing matched."}</p>;

    return (
        <Canvas
            key={folderId}
            rows={rows}
            now={now}
            exhausted={exhausted || results !== null}
            loading={loading}
            selected={selected}
            onSelect={onSelect}
            onLoadMore={loadMore}
        />
    );
}

function Canvas(
    { rows, now, exhausted, loading, selected, onSelect, onLoadMore }: CanvasProps,
) {
    const scroller = useRef<HTMLDivElement | null>(null);
    const pinned = useRef<number | null>(null);

    const headers = useMemo(
        () => rows.flatMap((row, index) => (row.kind === "header" ? [index] : [])),
        [rows],
    );

    const rangeExtractor = useCallback((range: Range) => {
        const active = activeHeader(headers, range.startIndex);
        pinned.current = active;

        if (active === null) return defaultRangeExtractor(range);

        const indexes = new Set(defaultRangeExtractor(range));
        indexes.add(active);

        return [...indexes].sort((a, b) => a - b);
    }, [headers]);

    const virtualizer = useVirtualizer<HTMLDivElement, HTMLElement>({
        count: rows.length,
        getScrollElement: () => scroller.current,
        estimateSize: (index) => (rows[index]?.kind === "header" ? HEADER_HEIGHT : ROW_HEIGHT),
        getItemKey: (index) => rows[index]?.key ?? index,
        overscan: OVERSCAN,
        rangeExtractor,
    });

    const items = virtualizer.getVirtualItems();
    const last = items.length === 0 ? -1 : items[items.length - 1].index;

    useEffect(() => {
        if (exhausted || loading || rows.length === 0) return;
        if (last < rows.length - 1 - OVERSCAN) return;

        onLoadMore();
    }, [last, rows.length, exhausted, loading, onLoadMore]);

    // a message restored from the url is usually off screen, so bring it into view once. only
    // once: after that the reader follows the list rather than the list chasing the reader, and
    // it stays put if the selection is somewhere the loaded window has not reached
    const restored = useRef(false);

    useEffect(() => {
        if (restored.current || selected === null || rows.length === 0) return;

        const index = rows.findIndex(
            (row) => row.kind === "message" && row.message.id === selected);
        if (index < 0) return;

        restored.current = true;
        if (index > 0) virtualizer.scrollToIndex(index, { align: "center" });
    }, [rows, selected, virtualizer]);

    return (
        <div class="message-list" ref={scroller}>
            <div class="message-list-canvas" style={{ height: `${virtualizer.getTotalSize()}px` }}>
                {items.map((item) => {
                    const row = rows[item.index];
                    if (row === undefined) return null;

                    const header = row.kind === "header";
                    const stuck = header && item.index === pinned.current;

                    const placement = {
                        left: 0,
                        width: "100%",
                        ...(header ? { zIndex: 1 } : {}),
                        ...(stuck
                            ? { position: "sticky" as const, top: 0 }
                            : {
                                position: "absolute" as const,
                                top: 0,
                                transform: `translateY(${item.start}px)`,
                            }),
                    };

                    if (row.kind === "header") {
                        return (
                            <div
                                key={item.key}
                                data-row
                                data-index={item.index}
                                ref={virtualizer.measureElement}
                                class="message-group"
                                style={placement}
                            >
                                {row.label}
                            </div>
                        );
                    }

                    const classes = ["message-row"];
                    if (!row.message.read) classes.push("unread");
                    if (row.first) classes.push("group-start");
                    if (row.message.id === selected) classes.push("on");

                    return (
                        <button
                            key={item.key}
                            type="button"
                            data-row
                            data-index={item.index}
                            ref={virtualizer.measureElement}
                            class={classes.join(" ")}
                            aria-current={row.message.id === selected ? "true" : undefined}
                            onClick={() => { onSelect(row.message); }}
                            style={placement}
                        >
                            <MessageRow message={row.message} now={now} />
                        </button>
                    );
                })}
            </div>
        </div>
    );
}
