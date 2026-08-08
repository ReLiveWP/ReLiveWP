import { useCallback, useEffect } from "preact/hooks";
import { useSignal } from "@preact/signals";

import { ENDPOINT_BEGIN_ACCOUNT_LINKING, ENDPOINT_LINK_CREDENTIALS, ENDPOINT_UPDATE_LINK } from "~/util/endpoints";
import { useAuthenticatedFetch } from "~/state/app-state";
import { getCredentialServiceConfig, getServiceConfig, requiresHandle } from "./service-config";
import { useLinkAccount } from "./link-account-context";
import { useLinkedAccounts } from "../../state/linked-accounts";
import { ServiceCaps, ServiceCapNames } from "~/util/service-caps";

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

export function CredentialsStage() {
    const fetch = useAuthenticatedFetch();
    const { error, service, stage, connectionId, isRelink, onClose } = useLinkAccount();
    const config = getCredentialServiceConfig(service);

    const serviceUrl = useSignal("");
    const username = useSignal("");
    const secret = useSignal("");
    const label = useSignal("");
    const busy = useSignal(false);

    if (!config) return null;

    const onSubmit = async (e: SubmitEvent) => {
        e.preventDefault();
        if (busy.value) return;

        busy.value = true;
        error.value = null;

        try {
            const response = await fetch(ENDPOINT_LINK_CREDENTIALS, {
                method: 'POST',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    service,
                    service_url: serviceUrl.value,
                    username: username.value,
                    secret: secret.value,
                    label: label.value,
                    connection_id: connectionId.value || null,
                }),
            });

            if (!response.ok) {
                const detail = await response.text();
                error.value = detail?.trim() || "We couldn't connect to that server.";
                return;
            }

            const { connection_id } = await response.json();
            connectionId.value = connection_id;

            stage.value = isRelink ? 'done' : 'configure';
        } catch {
            error.value = "We couldn't connect to that server.";
        } finally {
            busy.value = false;
        }
    };

    return (
        <form onSubmit={onSubmit}>
            <h1>where is it?</h1>
            {isRelink && <p>Please re-enter your username and password.</p>}
            <label htmlFor="serviceUrl">{config.urlTitle}</label>
            <input
                id="serviceUrl"
                type="text"
                class="textbox"
                placeholder={config.urlPlaceholder}
                value={serviceUrl}
                onChange={(e) => { serviceUrl.value = e.currentTarget.value; }}
            />
            <label htmlFor="username">{config.usernameTitle}</label>
            <input
                id="username"
                type="text"
                class="textbox"
                autocomplete="off"
                value={username}
                onChange={(e) => { username.value = e.currentTarget.value; }}
            />
            <label htmlFor="secret">{config.secretTitle}</label>
            <input
                id="secret"
                type="password"
                class="textbox"
                autocomplete="off"
                value={secret}
                onChange={(e) => { secret.value = e.currentTarget.value; }}
            />
            <label htmlFor="label">{config.labelTitle}</label>
            <input
                id="label"
                type="text"
                class="textbox"
                placeholder={config.labelPlaceholder}
                value={label}
                onChange={(e) => { label.value = e.currentTarget.value; }}
            />
            <p>Your server must be accessible to the open internet via HTTPS.</p>
            {error.value && <p class="error">{error}</p>}
            <div class="buttons">
                <input type="submit" class="submit" value={busy.value ? "connecting..." : "connect"} disabled={busy.value} />
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
            try {
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
            } catch {
                error.value = "Something went wrong, try linking again later.";
                stage.value = 'error';
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

export function ConfigureStage() {
    const fetch = useAuthenticatedFetch();
    const { service, connectionId, stage, error, initialCaps, currentEnabledCaps, onClose } = useLinkAccount();
    const { availableLinks, doRefresh } = useLinkedAccounts();

    const isReconfigure = currentEnabledCaps !== undefined;

    const serviceInfo = (availableLinks.value ?? []).find(l => l.service === service);
    const availableCaps = serviceInfo
        ? Object.entries(ServiceCapNames)
            .map(([cap]) => Number(cap) as ServiceCaps)
            .filter(cap => cap !== ServiceCaps.none && (serviceInfo.capabilities & cap) !== 0)
        : [];

    const availableCapsSet = serviceInfo?.capabilities ?? 0;
    const enabledCaps = useSignal(
        isReconfigure ? currentEnabledCaps & availableCapsSet :
        initialCaps !== undefined ? initialCaps & availableCapsSet :
        availableCapsSet
    );

    const toggleCap = useCallback((cap: ServiceCaps) => {
        enabledCaps.value = enabledCaps.value ^ cap;
    }, []);

    const onSubmit = useCallback(async (e: SubmitEvent) => {
        e.preventDefault();
        stage.value = 'applying';

        const url = `${ENDPOINT_UPDATE_LINK}?connectionId=${encodeURIComponent(connectionId.value)}`;
        const res = await fetch(url, {
            method: 'PATCH',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ enabled_capabilities: enabledCaps.value }),
        });

        if (!res.ok) {
            error.value = "Something went wrong saving your preferences.";
            stage.value = 'error';
        } else if (isReconfigure) {
            doRefresh();
            onClose();
        } else {
            stage.value = 'done';
        }
    }, []);

    return (
        <form onSubmit={onSubmit}>
            <h1>almost there!</h1>
            <p>What do you want to use this account for?</p>
            {availableCaps.map(cap => (
                <label key={cap} for={ServiceCapNames[cap]} class="checkbox block">
                    <input
                        id={ServiceCapNames[cap]}
                        type="checkbox"
                        checked={(enabledCaps.value & cap) !== 0}
                        onChange={() => toggleCap(cap)}
                    />
                    {ServiceCapNames[cap as keyof typeof ServiceCapNames]}
                </label>
            ))}
            <input type="submit" class="submit" value="finish" />
        </form>
    )
}

export function ApplyStage() {
    return (
        <>
            <h1>working...</h1>
            <p>We're finishing up, hang tight...</p>
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
            <h1>sorry! something went wrong.</h1>
            <p class="error">{error}</p>
            <button onClick={onClose}>close</button>
        </>
    );
}
