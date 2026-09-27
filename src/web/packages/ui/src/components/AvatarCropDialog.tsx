import { useCallback, useEffect, useRef, useState } from "preact/hooks";

import { Dialog } from "./Dialog.tsx";

// pixels of the EXIF-oriented source, which is what the browser renders and so what the user framed
export type AvatarCrop = { x: number, y: number, size: number };

export type AvatarCropDialogProps = {
    file: File;
    onClose: () => void;
    // resolve to accept and close, reject to keep the dialog open and show the reason
    onConfirm: (file: File, crop: AvatarCrop) => Promise<void>;
    title?: string;
    hint?: string;
    class?: string;
    viewport?: number;
    maxZoom?: number;
};

const DEFAULT_VIEWPORT = 256;
const DEFAULT_MAX_ZOOM = 4;

export function AvatarCropDialog({
    file,
    onClose,
    onConfirm,
    title = "crop your picture",
    hint = "drag to move, use the slider to zoom.",
    class: className,
    viewport = DEFAULT_VIEWPORT,
    maxZoom = DEFAULT_MAX_ZOOM,
}: AvatarCropDialogProps) {
    const [url, setUrl] = useState<string | null>(null);
    const [natural, setNatural] = useState<{ width: number, height: number } | null>(null);
    const [zoom, setZoom] = useState(1);
    const [offset, setOffset] = useState({ x: 0, y: 0 });
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const drag = useRef<{ x: number, y: number } | null>(null);

    useEffect(() => {
        const objectUrl = URL.createObjectURL(file);
        setUrl(objectUrl);
        return () => URL.revokeObjectURL(objectUrl);
    }, [file]);

    // the shortest side fills the viewport at zoom 1, so the image always covers it
    const base = natural ? viewport / Math.min(natural.width, natural.height) : 1;
    const scale = base * zoom;
    const rendered = natural
        ? { width: natural.width * scale, height: natural.height * scale }
        : { width: viewport, height: viewport };

    const clamp = useCallback((next: { x: number, y: number }) => {
        const slackX = Math.max(0, (rendered.width - viewport) / 2);
        const slackY = Math.max(0, (rendered.height - viewport) / 2);
        return {
            x: Math.min(slackX, Math.max(-slackX, next.x)),
            y: Math.min(slackY, Math.max(-slackY, next.y)),
        };
    }, [rendered.width, rendered.height, viewport]);

    useEffect(() => setOffset(o => clamp(o)), [clamp]);

    const onPointerDown = (e: PointerEvent) => {
        (e.currentTarget as HTMLElement).setPointerCapture(e.pointerId);
        drag.current = { x: e.clientX - offset.x, y: e.clientY - offset.y };
    };

    const onPointerMove = (e: PointerEvent) => {
        if (!drag.current) return;
        setOffset(clamp({ x: e.clientX - drag.current.x, y: e.clientY - drag.current.y }));
    };

    const onPointerUp = (e: PointerEvent) => {
        (e.currentTarget as HTMLElement).releasePointerCapture(e.pointerId);
        drag.current = null;
    };

    const confirm = useCallback(async () => {
        if (!natural) return;

        setBusy(true);
        setError(null);

        try {
            // viewport geometry back into source pixels. The image is centred, then offset, so the
            // viewport's top-left sits at half the overhang minus the pan.
            const crop: AvatarCrop = {
                x: Math.max(0, Math.round(((rendered.width - viewport) / 2 - offset.x) / scale)),
                y: Math.max(0, Math.round(((rendered.height - viewport) / 2 - offset.y) / scale)),
                size: Math.max(1, Math.round(viewport / scale)),
            };

            await onConfirm(file, crop);
            onClose();
        }
        catch (e) {
            setError((e as Error)?.message || "that image could not be used, try another.");
        }
        finally {
            setBusy(false);
        }
    }, [file, natural, scale, rendered.width, rendered.height, offset.x, offset.y, viewport, onConfirm, onClose]);

    const classes = ["avatar-crop-dialog"];
    if (className) classes.push(className);
    if (busy) classes.push("disabled");

    return (
        <Dialog class={classes.join(" ")} onClose={onClose}>
            <h1>{title}</h1>
            <p>{hint}</p>

            <div
                class="avatar-crop-viewport"
                style={{ width: viewport, height: viewport }}
                onPointerDown={onPointerDown}
                onPointerMove={onPointerMove}
                onPointerUp={onPointerUp}
                onPointerCancel={onPointerUp}>
                {url && (
                    <img
                        src={url}
                        alt=""
                        draggable={false}
                        onLoad={e => setNatural({
                            width: (e.currentTarget as HTMLImageElement).naturalWidth,
                            height: (e.currentTarget as HTMLImageElement).naturalHeight,
                        })}
                        style={{
                            width: rendered.width,
                            height: rendered.height,
                            transform: `translate(-50%, -50%) translate(${offset.x}px, ${offset.y}px)`,
                        }} />
                )}
            </div>

            <input
                type="range"
                min={1}
                max={maxZoom}
                step={0.01}
                value={zoom}
                style={{ width: viewport }}
                onInput={e => setZoom(Number((e.currentTarget as HTMLInputElement).value))} />

            {error && <p class="error">{error}</p>}

            <div class="buttons">
                <button onClick={() => confirm()} disabled={!natural || busy}>save</button>
                <button onClick={() => onClose()}>cancel</button>
            </div>
        </Dialog>
    );
}
