export const HostProps = {
    AuthenticationBuffer: "AuthenticationBuffer",
    DAToken: "DAToken",
    DASessionKey: "DASessionKey",
    DAStartTime: "DAStartTime",
    DAExpires: "DAExpires",
    STSInlineFlowToken: "STSInlineFlowToken",
    CID: "CID",
    PUID: "PUID",
    Username: "Username",
    SigninName: "SigninName",
    FirstName: "FirstName",
    LastName: "LastName",
    ErrorCode: "ErrorCode",
    ErrorString: "ErrorString",
    ExtendedErrorString: "ExtendedErrorString",
    ErrorURL: "ErrorURL",
} as const;

function handlers(): WebkitMessageHandlers | undefined {
    try {
        return window.webkit && window.webkit.messageHandlers ? window.webkit.messageHandlers : undefined;
    } catch (e) {
        return undefined;
    }
}

// IE-style- COM property assignment
type PutFn = (k: string, v: string) => void;
type PropertyPut = { Property(k: string, v: string): void };

const rawPut: PutFn | null = (function () {
    try {
        return new Function("k", "v", "window.external.Property(k)=v") as PutFn;
    } catch (e) {
        return null;
    }
})();

function putProp(name: string, value: string): void {
    if (rawPut) {
        try { rawPut(name, value); return; } catch (e) { /* not Trident: put threw at runtime */ }
    }
    try { (window.external as unknown as PropertyPut).Property(name, value); } catch (e) { /* no host */ }
}

export function setProperty(name: string, value: string): void {
    const h = handlers();
    if (h) {
        const o: { [k: string]: string } = {};
        o[name] = value;
        h.Property.postMessage(JSON.stringify(o));
        return;
    }
    putProp(name, value);
}

export function getProperty(name: string): string | null {
    try {
        return window.external.Property(name);
    } catch (e) {
        return null;
    }
}

export function setWizardButtons(back: boolean, next: boolean, lastPage: boolean): void {
    const h = handlers();
    try {
        if (h) h.SetWizardButtons.postMessage(JSON.stringify({ IsBackEnabled: back, IsNextEnabled: next, IsLastPage: lastPage }));
        else window.external.SetWizardButtons(back, next, lastPage);
    } catch (e) { /* no host */ }
}

export function setHeaderText(text: string): void {
    const h = handlers();
    try {
        if (h) h.SetHeaderText.postMessage(text);
        else window.external.SetHeaderText(text, "");
    } catch (e) { /* no host */ }
}

export function finalNext(): void {
    const h = handlers();
    try {
        if (h) h.FinalNext.postMessage("");
        else window.external.FinalNext();
    } catch (e) { /* no host */ }
}

export function finalBack(): void {
    const h = handlers();
    try {
        if (h) h.FinalBack.postMessage("");
        else window.external.FinalBack();
    } catch (e) { /* no host */ }
}
