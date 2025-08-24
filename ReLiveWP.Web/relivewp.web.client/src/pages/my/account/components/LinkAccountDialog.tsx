import "./link-account-dialog.scss"

import { Signal, useSignal } from "@preact/signals";
import { useEffect, useLayoutEffect, useRef, useState } from "preact/hooks"

import { ENDPOINT_BEGIN_ACCOUNT_LINKING } from "~/util/endpoints";
import { useAppState } from "~/state/app-state";

type Stage = 1 | 2 | 3 | 4 | -1;

type LinkAccountState = {
    handle: Signal<string>;
    redirectUrl: Signal<string>;
    state: Signal<Stage>;
    error: Signal<string>;
}

function LinkAccountDialogStage1({ onSubmit, handle, error, service }: LinkAccountState & { onSubmit: (e: SubmitEvent) => void, service: string }) {
    let title = <label htmlFor="handle">Bluesky Handle</label>;
    let placeholder = "@wamwoowam.co.uk";
    if (service == "mastodon" || service == "misskey") {
        title = <label htmlFor="handle">ActivityPub Handle</label>;
        placeholder = "@wamwoowam@snug.moe"
    }

    return (
        <form onSubmit={onSubmit}>
            <h1>who are you?</h1>

            {title}
            <input id="handle"
                type="text"
                class="textbox"
                placeholder={placeholder}
                value={handle}
                onChange={(e) => handle.value = e.currentTarget.value} />

            {error ? <p class="error">{error}</p> : undefined}

            <input type="submit" class="submit" value="sign in" />
        </form>
    );
}

function LinkAccountDialogStage2({ service, handle, redirectUrl, error, state, showWindow }: LinkAccountState & { showWindow: Function, service: string }) {
    const { authenticatedFetch: { value: _fetch } } = useAppState();
    const beginAccountLinking = async () => {
        const response = await _fetch(ENDPOINT_BEGIN_ACCOUNT_LINKING, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ service, identifier: handle.value })
        });

        if (!response.ok) {
            if (["atproto", "misskey", "mastodon"].includes(service)) {
                error.value = "We couldn't find that handle, try again.";
                state.value = 1;
            }
            else {
                error.value = "Something went wrong, try linking again later.";
                state.value = -1;
            }
        }

        const { redirect_uri } = await response.json();

        state.value = 3;
        redirectUrl.value = redirect_uri;

        showWindow(redirect_uri);
    }

    useEffect(() => {
        beginAccountLinking();
    })

    return (
        <>
            <h1>working...</h1>
            <p>We're looking you up, hang tight...</p>
        </>
    );
}

function LinkAccountDialogStage3({ redirectUrl, state, showWindow }: LinkAccountState & { showWindow: Function }) {
    return (
        <>
            <h1>nothing to see here!</h1>
            <p>We're signing you in, you should see a new window. If not <a href="#" onClick={() => showWindow(redirectUrl.value)}>click here to open one manually.</a></p>
            <button onClick={() => state.value = 1}>cancel</button>
        </>
    );
}


function LinkAccountDialogErrorStage({ error, onClose, }: LinkAccountState & { onClose: Function }) {
    return (
        <>
            <h1>sorry! we couldn't do that</h1>
            <p class="error">{error}</p>
            <button onClick={() => onClose()}>close</button>
        </>
    );
}

function LinkAccountDialogStage4({ onFinish }: { onFinish: (e: SubmitEvent) => void }) {
    return (
        <form onSubmit={onFinish}>
            {/* <h1>what should this account do?</h1>
            <label htmlFor="email">
                <input id="email"
                    type="checkbox"
                    class="checkbox"></input>
                <span>email</span>
            </label>
            <label htmlFor="storage">
                <input id="storage"
                    type="checkbox"
                    class="checkbox"></input>
                <span>storage</span>
            </label>
            <label htmlFor="social">
                <input id="social"
                    type="checkbox"
                    class="checkbox"></input>
                <span>social</span>
            </label>
            <label htmlFor="media">
                <input id="media"
                    type="checkbox"
                    class="checkbox"></input>
                <span>media</span>
            </label> */}
            <h1>all done!</h1>
            <p>You can now use services from this account on your device. Have fun!</p>
            <input type="submit" class="submit" value="finish" />
        </form>
    );
}


export default function LinkAccountDialog({ isShown, onClose, service }
    : { isShown: boolean, onClose: Function, service: string }) {
    const dialogRef = useRef<HTMLDialogElement>(null)
    const [state, setState] = useState({
        handle: useSignal(""),
        redirectUrl: useSignal(""),
        state: useSignal<Stage>(1),
        error: useSignal<string>(null)
    })

    const stage = state.state;

    useLayoutEffect(() => {
        if (!["atproto", "mastodon", "misskey"].includes(service) && stage.value == 1 && !!service) {
            stage.value = 2;
        }
        else {
            stage.value = 1;
        }
    }, [service])

    useLayoutEffect(() => {
        if (dialogRef.current?.open && !isShown) {
            dialogRef.current?.close()
            state.state.value = 1;
            state.error.value = null;
        } else if (!dialogRef.current?.open && isShown) {
            dialogRef.current?.showModal()
        }
    }, [isShown])

    const onSubmit = async (e: SubmitEvent) => {
        e.preventDefault();
        stage.value = 2;
    }

    const onFinish = (e: SubmitEvent) => {
        e.preventDefault();
        onClose();
    }

    const showWindow = (redirectUri: string) => {
        const handle = window.open(
            redirectUri,
            "oauthWindow",
        );

        const channel = new BroadcastChannel("a0eb0210-bc9a-4bc5-be15-44ff49b71027");
        const onMessage = () => {
            handle.close();
            stage.value = 4;
            channel.removeEventListener("message", onMessage);
        }

        channel.addEventListener("message", onMessage);
    }

    const StateMachine = ({ state: s }: { state: Stage }) => {
        switch (s) {
            case 1:
                return (
                    <LinkAccountDialogStage1 {...state} service={service} onSubmit={onSubmit} />
                );
            case 2:
                return (
                    <LinkAccountDialogStage2 {...state} service={service} showWindow={showWindow} />
                );
            case 3:
                return (
                    <LinkAccountDialogStage3 {...state} showWindow={showWindow} />
                )
            case 4:
                return (
                    <LinkAccountDialogStage4 {...state} onFinish={onFinish} />
                );
            case -1:
                return (
                    <LinkAccountDialogErrorStage {...state} onClose={onClose} />
                );
            default:
                return <span>{s}</span>
        }
    }


    return (
        <dialog ref={dialogRef}
            class="link-account-dialog"
            onClose={() => onClose()}>
            {isShown && <StateMachine state={stage.value} />}
        </dialog>
    )
}