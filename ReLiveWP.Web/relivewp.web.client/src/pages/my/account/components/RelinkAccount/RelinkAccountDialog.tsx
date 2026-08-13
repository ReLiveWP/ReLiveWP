import "../link-account-dialog.scss";

import { useSignal } from "@preact/signals";
import { useEffect } from "preact/hooks";

import { Dialog } from "~/components/Dialog";
import { useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_RELINK_ACCOUNT } from "~/util/endpoints";
import { type Stage } from "../LinkAccount/service-config";
import { LinkAccountContext, useLinkAccount } from "../LinkAccount/link-account-context";
import { DoneStage, ErrorStage, RedirectStage } from "../LinkAccount/LinkAccountStages";
import { OAUTH_CHANNEL } from "../LinkAccount/LinkAccountDialog";

function RelinkLoadingStage() {
    const fetch = useAuthenticatedFetch();
    const { id, redirectUrl, error, stage } = useLinkAccount();

    useEffect(() => {
        const run = async () => {
            try {
                const response = await fetch(ENDPOINT_RELINK_ACCOUNT, {
                    method: 'POST',
                    headers: {
                        'Accept': 'application/json',
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ connection_id: id }),
                });

                if (!response.ok) {
                    error.value = "Something went wrong, try fixing this account later.";
                    stage.value = 'error';
                } else {
                    const { redirect_uri } = await response.json();
                    redirectUrl.value = redirect_uri;
                    stage.value = 'redirect';
                    window.open(redirect_uri, "_blank");
                }
            } catch {
                error.value = "Something went wrong, try fixing this account later.";
                stage.value = 'error';
            }
        };

        run();
    }, []);

    return (
        <>
            <h1>working...</h1>
            <p>We're getting things ready, hang tight...</p>
        </>
    );
}

export default function RelinkAccountDialog({ id, onClose }: {
    id: string;
    onClose: () => void;
}) {
    const handle = useSignal("");
    const redirectUrl = useSignal("");
    const service = useSignal("");
    const stage = useSignal<Stage>('loading');
    const error = useSignal<string | null>(null);
    const connectionId = useSignal("");

    useEffect(() => {
        const channel = new BroadcastChannel(OAUTH_CHANNEL);
        const onMessage = () => { stage.value = 'done'; };
        channel.addEventListener("message", onMessage);
        return () => {
            channel.removeEventListener("message", onMessage);
            channel.close();
        };
    }, []);

    const renderStage = () => {
        switch (stage.value) {
            case 'loading': return <RelinkLoadingStage />;
            case 'redirect': return <RedirectStage />;
            case 'done': return <DoneStage />;
            case 'error': return <ErrorStage />;
        }
    };

    return (
        <LinkAccountContext.Provider value={{ handle, redirectUrl, stage, error, connectionId, id, service, onClose }}>
            <Dialog class="link-account-dialog" onClose={onClose}>
                {renderStage()}
            </Dialog>
        </LinkAccountContext.Provider>
    );
}
