import "../link-account-dialog.scss";

import { useSignal } from "@preact/signals";
import { useEffect, useLayoutEffect } from "preact/hooks";

import { Dialog } from "~/components/Dialog";
import { type Stage, requiresHandle } from "./service-config";
import { LinkAccountContext } from "./link-account-context";
import { HandleStage, LoadingStage, RedirectStage, DoneStage, ErrorStage, ConfigureStage, ApplyStage } from "./LinkAccountStages";

export { OAUTH_CHANNEL } from "~/util/oauth";
import { OAUTH_CHANNEL } from "~/util/oauth";

export default function LinkAccountDialog({ onClose, service }: {
    onClose: () => void;
    service: string;
}) {
    const handle = useSignal("");
    const redirectUrl = useSignal("");
    const stage = useSignal<Stage>(requiresHandle(service) ? 'handle' : 'loading');
    const error = useSignal<string | null>(null);
    const connectionId = useSignal("");

    useLayoutEffect(() => {
        stage.value = requiresHandle(service) ? 'handle' : 'loading';
    }, [service]);

    useEffect(() => {
        const channel = new BroadcastChannel(OAUTH_CHANNEL);
        const onMessage = (e: MessageEvent<{ connectionId: string }>) => {
            connectionId.value = e.data.connectionId;
            stage.value = 'configure';
        };
        channel.addEventListener("message", onMessage);
        return () => {
            channel.removeEventListener("message", onMessage);
            channel.close();
        };
    }, []);

    const renderStage = () => {
        switch (stage.value) {
            case 'handle': return <HandleStage />;
            case 'loading': return <LoadingStage />;
            case 'redirect': return <RedirectStage />;
            case "configure": return <ConfigureStage />;
            case "applying": return <ApplyStage/>
            case 'done': return <DoneStage />;
            case 'error': return <ErrorStage />;
        }
    };

    return (
        <LinkAccountContext.Provider value={{ handle, redirectUrl, stage, error, connectionId, service, onClose }}>
            <Dialog class="link-account-dialog" onClose={onClose}>
                {renderStage()}
            </Dialog>
        </LinkAccountContext.Provider>
    );
}
