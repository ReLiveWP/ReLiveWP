import type { Contact } from "@relivewp/eas-store";
import { defaultRangeExtractor, type Range } from "@tanstack/virtual-core";
import { useCallback, useEffect, useMemo, useRef, useState } from "preact/hooks";

import { useMedia } from "~/hooks/useMedia";
import { useVirtualizer } from "~/hooks/useVirtualizer";
import AlphabetIndex from "./AlphabetIndex";
import ContactName from "./ContactName";
import { initialOf, letterIndex, toRows, type Row } from "./groups";

const ROW_HEIGHT = 46;
const HEADER_HEIGHT = 34;
const FIRST_HEADER_HEIGHT = 18;
const OVERSCAN = 10;
const NARROW = "(max-width: 1100px)";

type Placement = Record<string, string | number>;

type Props = {
    contacts: Contact[],
    photos: ReadonlyMap<string, string>,
    selected: string | null,
    searching: boolean,
    onSelect: (contact: Contact) => void,
};

function heightOf(rows: Row[], index: number): number {
    const row = rows[index];
    if (row === undefined || row.kind !== "header") return ROW_HEIGHT;

    return index === 0 ? FIRST_HEADER_HEIGHT : HEADER_HEIGHT;
}

function activeHeader(headers: number[], startIndex: number): number | null {
    let found: number | null = null;

    for (const index of headers) {
        if (index > startIndex) break;
        found = index;
    }

    return found;
}

const headerId = (letter: string) => `contact-letter-${letter === "#" ? "other" : letter}`;

function scrollParent(from: Element | null): Element | null {
    for (let at = from; at !== null; at = at.parentElement) {
        const overflow = getComputedStyle(at).overflowY;
        if ((overflow === "auto" || overflow === "scroll") && at.scrollHeight > at.clientHeight)
            return at;
    }

    return null;
}

function Avatar({ contact, url }: { contact: Contact, url: string | undefined }) {
    if (url === undefined) return <span class="contact-tile">{initialOf(contact)}</span>;

    return <img class="contact-tile" src={url} alt="" loading="lazy" decoding="async" />;
}

type ListHeaderProps = {
    entry: Extract<Row, { kind: "header" }>,
    at: number,
    style?: Placement
}


const ListHeader = ({ entry, at, style }: ListHeaderProps) => {
    const classes = ["contact-group"];
    if (at === 0) classes.push("first");

    return (
        <div id={headerId(entry.letter)} class={classes.join(" ")} style={style}>
            {entry.letter}
        </div>
    );
};

type ListPersonProps = {
    entry: Extract<Row, { kind: "contact" }>,
    style?: Placement,
    isSelected: boolean,
    onSelect: (contact: Contact) => void,
    photos: ReadonlyMap<string, string>,
};

const ListPerson = ({ entry, style, isSelected, onSelect, photos }: ListPersonProps) => {
    const classes = ["contact-row"];
    if (isSelected) classes.push("on");

    return (
        <button
            type="button"
            class={classes.join(" ")}
            aria-current={isSelected ? "true" : undefined}
            onClick={() => { onSelect(entry.contact); }}
            style={style}
        >
            <Avatar contact={entry.contact} url={photos.get(entry.contact.id)} />
            <span class="contact-name"><ContactName contact={entry.contact} /></span>
            <span class="contact-badge" />
        </button>
    );
};

export default function ContactList(
    { contacts, photos, selected, searching, onSelect }: Props,
) {
    const scroller = useRef<HTMLDivElement | null>(null);
    const pinned = useRef<number | null>(null);
    const narrow = useMedia(NARROW);
    const [progress, setProgress] = useState(0);

    const rows = useMemo(() => toRows(contacts), [contacts]);
    const index = useMemo(() => (searching ? [] : letterIndex(rows)), [rows, searching]);

    const headers = useMemo(
        () => rows.flatMap((row, at) => (row.kind === "header" ? [at] : [])),
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
        count: narrow ? 0 : rows.length,
        getScrollElement: () => scroller.current,
        estimateSize: (at) => heightOf(rows, at),
        getItemKey: (at) => rows[at]?.key ?? at,
        overscan: OVERSCAN,
        rangeExtractor,
    });

    // whichever element actually scrolls: the rail's own list when it has a scrollport, the page
    // once it has given that up
    useEffect(() => {
        const scrolling = scrollParent(scroller.current);
        if (scrolling === null) return;

        const read = () => {
            const span = scrolling.scrollHeight - scrolling.clientHeight;
            setProgress(span <= 0 ? 0 : scrolling.scrollTop / span);
        };

        read();
        scrolling.addEventListener("scroll", read, { passive: true });

        return () => { scrolling.removeEventListener("scroll", read); };
    }, [narrow, rows.length]);

    const jump = useCallback((row: number) => {
        const target = rows[row];
        if (target === undefined || target.kind !== "header") return;

        if (!narrow) {
            virtualizer.scrollToIndex(row, { align: "start" });
            return;
        }

        document.getElementById(headerId(target.letter))
            ?.scrollIntoView({ block: "start", behavior: "smooth" });
    }, [narrow, rows, virtualizer]);

    if (rows.length === 0)
        return <p class="note">{searching ? "Nothing matched." : "No contacts yet."}</p>;

    return (
        <div class="contact-rail-body">
            <div class="contact-list" ref={scroller}>
                {narrow
                    ? (
                        <div class="contact-list-flow">
                            {rows.map((entry, at) => (entry.kind === "header"
                                ? <ListHeader key={entry.key} entry={entry} at={at} />
                                : <ListPerson key={entry.key}
                                    entry={entry}
                                    isSelected={entry.contact.id === selected}
                                    onSelect={onSelect}
                                    photos={photos} />))}
                        </div>
                    )
                    : (
                        <div
                            class="contact-list-canvas"
                            style={{ height: `${virtualizer.getTotalSize()}px` }}
                        >
                            {virtualizer.getVirtualItems().map((item) => {
                                const entry = rows[item.index];
                                if (entry === undefined) return null;

                                const stuck = entry.kind === "header" && item.index === pinned.current;
                                const key = String(item.key);

                                const style: Placement = stuck
                                    ? { position: "sticky", top: 0, zIndex: 1 }
                                    : {
                                        position: "absolute",
                                        top: 0,
                                        left: 0,
                                        width: "100%",
                                        transform: `translateY(${item.start}px)`,
                                    };

                                return entry.kind === "header"
                                    ? <ListHeader key={key} entry={entry} style={style} at={item.index} />
                                    : <ListPerson key={key}
                                        entry={entry}
                                        isSelected={entry.contact.id === selected}
                                        onSelect={onSelect}
                                        photos={photos} 
                                        style={style} />
                            })}
                        </div>
                    )}
            </div>

            {index.length > 0
                && <AlphabetIndex entries={index} progress={progress} onJump={jump} />}
        </div>
    );
}
