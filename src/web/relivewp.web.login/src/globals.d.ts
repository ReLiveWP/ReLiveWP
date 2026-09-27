// each property has a fixed VARTYPE the host checks, see docs/wp81-inline-login.md
type PropertyValue = string | number | boolean;

interface External {
    Property(name: string): PropertyValue | null;
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

interface InlineFormData {
    formId: string;
    submitId: string;
    headerText: string;
}

interface InlineErrorData {
    code: number;
    message: string;
    url?: string | null;
}

interface InlineWizardData {
    headerText: string;
}

interface InlineServerData {
    form?: InlineFormData;
    wizard?: InlineWizardData;
    error?: InlineErrorData | null;
    properties?: { [name: string]: string | null };
}

interface Window {
    webkit?: { messageHandlers?: WebkitMessageHandlers };
    // host calls these when the user taps native Back/Next. it also probes for OnNext/OnFinish at
    // DocumentComplete to decide whether the document is a valid wizard page
    OnNext?: () => void;
    OnBack?: () => void;
    OnFinish?: () => void;
    __INLINE__?: InlineServerData;
}
