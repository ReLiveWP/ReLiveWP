import { useEffect } from "preact/hooks";

import { ENDPOINT_BEGIN_ACCOUNT_LINKING } from "~/util/endpoints";
import { useAppState, useAuthenticatedFetch } from "~/state/app-state";
import { getServiceConfig, requiresHandle } from "./service-config";
import { useLinkAccount } from "./link-account-context";
import { useLinkedAccounts } from "../../state/linked-accounts";

export function HandleStage() {
    const { handle, error, service, stage, onClose } = useLinkAccount();
    const config = getServiceConfig(service);
    if (!config) return null;

    const onSubmit = (e: SubmitEvent) => {
        e.preventDefault();
        stage.value = 'loading';
    };

    return (
        <form onSubmit={onSubmit}>
            <h1>who are you?</h1>
            <label htmlFor="handle">{config.title}</label>
            <input
                id="handle"
                type="text"
                class="textbox"
                placeholder={config.placeholder}
                value={handle}
                onChange={(e) => { handle.value = e.currentTarget.value; }}
            />
            {error.value && <p class="error">{error}</p>}
            <div class="buttons">
                <input type="submit" class="submit" value="sign in" />
                <button onClick={() => onClose()}>cancel</button>
            </div>
        </form>
    );
}

export function LoadingStage() {
    const fetch = useAuthenticatedFetch()
    const { handle, redirectUrl, error, stage, service } = useLinkAccount();

    useEffect(() => {
        const run = async () => {
            const response = await fetch(ENDPOINT_BEGIN_ACCOUNT_LINKING, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ service, identifier: handle.value }),
            });

            if (!response.ok) {
                if (requiresHandle(service)) {
                    error.value = "We couldn't find that handle, try again.";
                    stage.value = 'handle';
                } else {
                    error.value = "Something went wrong, try linking again later.";
                    stage.value = 'error';
                }
            } else {
                const { redirect_uri } = await response.json();
                redirectUrl.value = redirect_uri;
                stage.value = 'redirect';
                window.open(redirect_uri, "_blank");
            }
        };

        run();
    }, []);

    return (
        <>
            <h1>working...</h1>
            <p>We're looking you up, hang tight...</p>
        </>
    );
}

export function RedirectStage() {
    const { redirectUrl, onClose } = useLinkAccount();

    return (
        <>
            <h1>nothing to see here!</h1>
            <p>
                We're signing you in, you should see a new window.{" "}
                If not <a href={redirectUrl.value} target="_blank">click here to open one manually.</a>
            </p>
            <button onClick={onClose}>cancel</button>
        </>
    );
}

export function DoneStage() {
    const { onClose } = useLinkAccount();
    const { doRefresh } = useLinkedAccounts();

    const onFinish = (e: SubmitEvent) => {
        e.preventDefault();
        onClose();
        doRefresh();
    };

    return (
        <form onSubmit={onFinish}>
            <h1>all done!</h1>
            <p>You can now use services from this account on your device. Have fun!</p>
            <p>Please note that changes may take a few minutes to propagate.</p>
            <input type="submit" class="submit" value="finish" />
        </form>
    );
}

export function ErrorStage() {
    const { error, onClose } = useLinkAccount();

    return (
        <>
            <h1>sorry! we couldn't do that</h1>
            <p class="error">{error}</p>
            <button onClick={onClose}>close</button>
        </>
    );
}
