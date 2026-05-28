import "../link-account-dialog.scss";

import { useSignal } from "@preact/signals";
import { useEffect, useLayoutEffect } from "preact/hooks";

import { Dialog } from "~/components/Dialog";
import { type Stage, requiresHandle } from "./service-config";
import { LinkAccountContext } from "./link-account-context";
import { HandleStage, LoadingStage, RedirectStage, DoneStage, ErrorStage } from "./LinkAccountStages";

export const OAUTH_CHANNEL = "a0eb0210-bc9a-4bc5-be15-44ff49b71027";

export default function LinkAccountDialog({ onClose, service }: {
    onClose: () => void;
    service: string;
}) {
    const handle = useSignal("");
    const redirectUrl = useSignal("");
    const stage = useSignal<Stage>(requiresHandle(service) ? 'handle' : 'loading');
    const error = useSignal<string | null>(null);

    useLayoutEffect(() => {
        stage.value = requiresHandle(service) ? 'handle' : 'loading';
    }, [service]);

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
            case 'handle':   return <HandleStage />;
            case 'loading':  return <LoadingStage />;
            case 'redirect': return <RedirectStage />;
            case 'done':     return <DoneStage />;
            case 'error':    return <ErrorStage />;
        }
    };

    return (
        <LinkAccountContext.Provider value={{ handle, redirectUrl, stage, error, service, onClose }}>
            <Dialog class="link-account-dialog" onClose={onClose}>
                {renderStage()}
            </Dialog>
        </LinkAccountContext.Provider>
    );
}
