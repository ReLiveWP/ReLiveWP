import type { Body } from "@relivewp/eas-store";
import { useColourScheme } from "@relivewp/ui";
import { useEffect, useState } from "preact/hooks";

import { sanitise } from "./sanitise";

const TOKENS = ["--ink", "--ink-muted", "--rule", "--accent-colour", "--surface"];
const POLICY = "default-src 'none'; style-src 'unsafe-inline'; img-src data:";
const STYLE = `
    html{background:var(--surface);color:var(--ink)}
    body{margin:0;font-family:var(--frame-font);font-size:14px;line-height:1.55;
         overflow-wrap:break-word}
    a{color:var(--accent-colour)}
    img{max-width:100%;height:auto}
    blockquote{margin:0 0 0 12px;padding-left:12px;border-left:1px solid var(--rule);
               color:var(--ink-muted)}
    table{max-width:100%}
`;

function palette(): string {
    const styles = getComputedStyle(document.body);

    return TOKENS
        .map((token) => `${token}:${styles.getPropertyValue(token).trim()}`)
        .filter((declaration) => !declaration.endsWith(":"))
        .join(";");
}

function document_(html: string, scheme: string): string {
    const font = getComputedStyle(document.body).fontFamily;
    const root = `:root{color-scheme:${scheme};--frame-font:${font};${palette()}}`;

    return `<!doctype html><html><head><meta charset="utf-8">`
        + `<meta http-equiv="Content-Security-Policy" content="${POLICY}">`
        + `<style>${root}${STYLE}</style></head>`
        + `<body>${html}</body></html>`;
}

function Html({ content }: { content: string }) {
    const scheme = useColourScheme();
    const [frame, setFrame] = useState<string | null>(null);
    const [failed, setFailed] = useState(false);

    useEffect(() => {
        let live = true;
        setFailed(false);

        sanitise(content)
            .then((clean) => { if (live) setFrame(document_(clean, scheme)); })
            .catch(() => { if (live) setFailed(true); });

        return () => { live = false; };
    }, [content, scheme]);

    
    if (failed) return <p class="error">This message could not be displayed.</p>;
    if (frame === null) return <p class="note">loading...</p>;

    return <iframe class="message-frame" sandbox="" srcdoc={frame} title="message body" />;
}

export default function MessageBody({ body }: { body: Body | null }) {
    if (body === null) return <p class="note">This message has no body yet. Sync to fetch it.</p>;

    if (body.type === "html") return <Html content={body.content} />;

    return (
        <div class="message-text">
            {body.content}
            {body.truncated && <p class="note">Truncated by the server.</p>}
        </div>
    );
}
