import { createServer } from "node:http";
import { createReadStream } from "node:fs";
import { stat } from "node:fs/promises";
import { extname, join, normalize } from "node:path";

const ROOT = new URL("../dist/", import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, "$1");
const PORT = Number(process.env.PORT ?? 8084);

const TYPES = {
    ".css": "text/css",
    ".html": "text/html; charset=utf-8",
    ".js": "text/javascript",
    ".json": "application/json",
    ".map": "application/json",
    ".png": "image/png",
    ".svg": "image/svg+xml",
    ".webmanifest": "application/manifest+json",
};

function caching(name) {
    if (name === "sw.js" || name === "index.html" || name === "manifest.webmanifest")
        return "no-cache";

    return /\.[0-9a-f]{8,}\./.test(name) ? "public, max-age=31536000, immutable" : "no-cache";
}

async function resolve(pathname) {
    const relative = normalize(decodeURIComponent(pathname)).replace(/^([/\\])+/, "");
    if (relative.includes("..")) return null;

    const candidate = join(ROOT, relative);

    try {
        if ((await stat(candidate)).isFile()) return candidate;
    } catch {
        // falls through to the spa shell
    }

    return null;
}

createServer(async (request, response) => {
    const { pathname } = new URL(request.url ?? "/", "http://localhost");
    const file = await resolve(pathname) ?? join(ROOT, "index.html");
    const name = file.slice(ROOT.length).replace(/\\/g, "/");

    response.setHeader("Content-Type", TYPES[extname(file)] ?? "application/octet-stream");
    response.setHeader("Cache-Control", caching(name));

    createReadStream(file)
        .on("error", () => { response.writeHead(404).end("not found"); })
        .pipe(response);
}).listen(PORT, () => {
    console.log(`serving dist on http://localhost:${PORT}`);
});
