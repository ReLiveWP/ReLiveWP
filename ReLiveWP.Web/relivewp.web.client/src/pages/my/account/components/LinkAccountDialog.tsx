import { useLayoutEffect, useRef, useState } from "preact/hooks"

import "./link-account-dialog.scss"
import { useAppState } from "../../../../state/app-state";
import { ENDPOINT_BEGIN_ACCOUNT_LINKING } from "../../../../util/endpoints";
import { useSignal } from "@preact/signals";

export default function LinkAccountDialog({ isShown, onClose, service }: { isShown: boolean, onClose: Function, service: string }) {
    const dialogRef = useRef<HTMLDialogElement>(null)
    const handle = useSignal("");
    const redirectUrl = useSignal("")
    const state = useSignal<1 | 2 | 3 | 4>(1);
    const error = useSignal<string>(null)
    const { token } = useAppState();

    useLayoutEffect(() => {
        if (dialogRef.current?.open && !isShown) {
            dialogRef.current?.close()
        } else if (!dialogRef.current?.open && isShown) {
            dialogRef.current?.showModal()
        }
    }, [isShown])

    const onSubmit = async (e: Event) => {
        e.preventDefault();
        state.value = 2;

        const response = await fetch(ENDPOINT_BEGIN_ACCOUNT_LINKING, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token.value}`
            },
            body: JSON.stringify({ service, identifier: handle.value })
        });

        if (!response.ok) {
            error.value = "we couldn't find that handle, try again";
            state.value = 1;
        }

        const { redirect_uri } = await response.json();

        state.value = 3;
        redirectUrl.value = redirect_uri;

        showWindow(redirect_uri);
    }

    const onFinish = (e: Event) => {
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
            state.value = 4;
            channel.removeEventListener("message", onMessage);
        }

        channel.addEventListener("message", onMessage);
    }

    const StateMachine = ({ state: s }: { state: 1 | 2 | 3 | 4 }) => {
        switch (s) {
            case 1:
                return (
                    <form onSubmit={onSubmit}>
                        <h1>who are you?</h1>

                        <label htmlFor="handle">AtProto Handle:</label>
                        <input id="handle"
                            type="text"
                            class="textbox"
                            placeholder="@wamwoowam.co.uk"
                            value={handle}
                            onChange={(e) => handle.value = e.currentTarget.value} />

                        {error ? <p>{error}</p> : undefined}

                        <input type="submit" class="submit" value="sign in" />
                    </form>
                );
            case 2:
                return (
                    <>
                        <h1>working...</h1>
                        <p>We're looking you up, hang tight...</p>
                    </>
                );
            case 3:
                return (
                    <>
                        <h1>nothing to see here!</h1>
                        <p>We're signing you in, you should see a new window, if not <a href="#" onClick={() => showWindow(redirectUrl.value)}>click here to open one manually.</a></p>
                        <button onClick={() => state.value = 1}>cancel</button>
                    </>
                )
            case 4:
                return (
                    <form onSubmit={onFinish}>
                        <h1>what should this account do?</h1>
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
                        </label>

                        <input type="submit" class="submit" value="finish" />
                    </form>
                );
        }
    }


    return (
        <dialog ref={dialogRef}
            class="link-account-dialog"
            onClose={() => onClose()}>
            <StateMachine state={state.value} />
        </dialog>
    )
}