export interface InlineConfig {
    id: string;
    mkt: string;
    lc: string;
    opid: string;
    uaid: string;
    postUrl: string;
}

export function readConfig(): InlineConfig {
    const s: InlineServerData = window.__INLINE__ || {};
    const q = new URLSearchParams(window.location.search);
    const pick = (a: string | undefined, key: string, dflt: string) => a || q.get(key) || dflt;

    return {
        id: pick(s.id, "id", ""),
        mkt: pick(s.mkt, "mkt", "EN-US"),
        lc: pick(s.lc, "lc", ""),
        opid: pick(s.opid, "opid", ""),
        uaid: pick(s.uaid, "uaid", ""),
        postUrl: s.postUrl || "/auth/inline_login",
    };
}
