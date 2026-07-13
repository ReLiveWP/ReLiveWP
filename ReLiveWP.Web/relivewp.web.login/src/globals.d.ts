interface External {
    Property(name: string): string | null;
    SetWizardButtons(back: boolean, next: boolean, lastPage: boolean): void;
    SetHeaderText(text: string, subtext?: string): void;
    FinalNext(): void;
    FinalBack(): void;
    Ready(): void;
    NotReady(): void;
    
    RequestStatus?: number;
    WebFlowUrl?: string;
    NotifyIdentityChanged?(): void;
    ReturnToApp?(): void;
    Submit?(): void;
    BrowseToAuthUI?(): void;

    notify?(json: string): void;
}

// EdgeHTML
interface WebkitMessageHandlers {
    Property: { postMessage(json: string): void };
    FinalNext: { postMessage(json: string): void };
    FinalBack: { postMessage(json: string): void };
    SetWizardButtons: { postMessage(json: string): void };
    SetHeaderText: { postMessage(json: string): void };
}

interface InlineServerData {
    id?: string;
    mkt?: string;
    lc?: string;
    opid?: string;
    uaid?: string;
    postUrl?: string;        // same-origin /auth/inline_login
}

interface Window {
    webkit?: { messageHandlers?: WebkitMessageHandlers };
    // host calls these when the user taps native Back/Next
    OnNext?: () => void;
    OnBack?: () => void;
    __INLINE__?: InlineServerData;
}
