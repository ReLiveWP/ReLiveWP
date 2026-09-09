import { useLayoutEffect, useRef, useState } from "preact/hooks";

import type { IndexEntry } from "../state/groups";

type Props = {
    entries: IndexEntry[],
    // how far through the list we are, 0 to 1
    progress: number,
    onJump: (row: number) => void,
};

export default function AlphabetIndex({ entries, progress, onJump }: Props) {
    const frame = useRef<HTMLElement | null>(null);
    const track = useRef<HTMLDivElement | null>(null);
    const [overflow, setOverflow] = useState(0);

    useLayoutEffect(() => {
        const frameElement = frame.current;
        const trackElement = track.current;
        if (frameElement === null || trackElement === null) return;

        const measure = () => {
            const wanted = Math.max(0, trackElement.scrollHeight - frameElement.clientHeight);
            setOverflow((current) => (current === wanted ? current : wanted));
        };

        measure();

        const observer = new ResizeObserver(measure);
        observer.observe(frameElement);
        observer.observe(trackElement);

        return () => { observer.disconnect(); };
    }, []);

    // slides only when it has to, so at any normal window height this is a no-op
    const offset = overflow * Math.min(1, Math.max(0, progress));

    return (
        <nav class="contact-index" aria-label="jump to a letter" ref={frame}>
            <div
                class="contact-index-track"
                ref={track}
                style={{ transform: `translateY(${-offset}px)` }}
            >
                {entries.map(({ letter, row }) => (
                    <button
                        key={letter}
                        type="button"
                        class={row === null ? "contact-index-letter empty" : "contact-index-letter"}
                        aria-disabled={row === null ? "true" : undefined}
                        onClick={() => { if (row !== null) onJump(row); }}
                    >
                        {letter}
                    </button>
                ))}
            </div>
        </nav>
    );
}
