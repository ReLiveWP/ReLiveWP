function handlers(): WebkitMessageHandlers | undefined {
    try {
        return window.webkit && window.webkit.messageHandlers ? window.webkit.messageHandlers : undefined;
    } catch (e) {
        return undefined;
    }
}

// neither TS nor a normal expression can emit assignment-to-call, and sloppy-mode engines
// only throw on the call, so build it once and find out whether it works by using it
var rawPut = (function () {
    try {
        return new Function("k", "v", "window.external.Property(k)=v") as (k: string, v: PropertyValue) => void;
    } catch (e) {
        return null;
    }
})();

function setProperty(name: string, value: PropertyValue): void {
    var h = handlers();
    if (h) {
        var payload: { [k: string]: PropertyValue } = {};
        payload[name] = value;
        h.Property.postMessage(JSON.stringify(payload));
        return;
    }

    if (rawPut) {
        try {
            rawPut(name, value);
            return;
        } catch (e) { /* not Trident */ }
    }

    try {
        (window.external as unknown as { Property(k: string, v: PropertyValue): void }).Property(name, value);
    } catch (e) { /* no host */ }
}

function setWizardButtons(back: boolean, next: boolean, lastPage: boolean): void {
    var h = handlers();
    try {
        if (h) h.SetWizardButtons.postMessage(JSON.stringify({ IsBackEnabled: back, IsNextEnabled: next, IsLastPage: lastPage }));
        else window.external.SetWizardButtons(back, next, lastPage);
    } catch (e) { /* no host */ }
}

function setHeaderText(text: string): void {
    var h = handlers();
    try {
        if (h) h.SetHeaderText.postMessage(text);
        else window.external.SetHeaderText(text, "");
    } catch (e) { /* no host */ }
}

function finalNext(): void {
    var h = handlers();
    try {
        if (h) h.FinalNext.postMessage("");
        else window.external.FinalNext();
    } catch (e) { /* no host */ }
}

function finalBack(): void {
    var h = handlers();
    try {
        if (h) h.FinalBack.postMessage("");
        else window.external.FinalBack();
    } catch (e) { /* no host */ }
}

// the host probes for OnNext/OnFinish at DocumentComplete, so nothing may navigate before then.
// see docs/wp81-inline-login.md
function whenDocumentComplete(action: () => void): void {
    if (document.readyState === "complete") {
        setTimeout(action, 0);
        return;
    }

    window.addEventListener("load", function () { setTimeout(action, 0); }, false);
}

function pushProperties(properties: { [name: string]: string | null }): void {
    for (var name in properties) {
        if (!Object.prototype.hasOwnProperty.call(properties, name)) continue;

        var value = properties[name];
        if (value !== null && value !== undefined) setProperty(name, value);
    }
}

function bindForm(form: InlineFormData, error?: InlineErrorData | null): void {
    if (error) {
        // ErrorCode is VT_I4, a string is rejected exactly like an unknown name would be
        setProperty("ErrorCode", error.code | 0);
        setProperty("ErrorString", error.message);
        if (error.url) setProperty("ErrorURL", error.url);
    }

    var element = document.getElementById(form.formId) as HTMLFormElement | null;
    var button = document.getElementById(form.submitId) as HTMLInputElement | null;
    var submitting = false;

    // the host sinks onsubmit on the form. form.submit() does not fire that, so OnNext has to go
    // through a real click, and we must never cancel the event
    var submit = function () {
        if (submitting || !button) return;
        button.click();
    };

    if (element) {
        element.onsubmit = function () {
            if (submitting) return false;
            submitting = true;
            setWizardButtons(false, false, true);
            return true;
        };
    }

    window.OnNext = submit;
    window.OnBack = finalBack;

    setHeaderText(form.headerText);
    setWizardButtons(false, true, true);
}

(function () {
    var data = window.__INLINE__;
    if (!data) return;

    // every document the host loads has to expose OnNext or OnFinish by DocumentComplete or it is
    // rejected as an invalid web document, see docs/wp81-inline-login.md
    window.OnNext = finalNext;
    window.OnFinish = finalNext;

    if (data.properties) {
        pushProperties(data.properties);
        whenDocumentComplete(finalNext);
        return;
    }

    if (data.wizard) {
        setHeaderText(data.wizard.headerText);
        setWizardButtons(false, true, true);
        return;
    }

    if (data.form) bindForm(data.form, data.error);
})();
