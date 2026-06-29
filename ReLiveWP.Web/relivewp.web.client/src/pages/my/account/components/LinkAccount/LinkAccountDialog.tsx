import "../link-account-dialog.scss";

import { useSignal } from "@preact/signals";
import { useEffect, useLayoutEffect } from "preact/hooks";

import { Dialog } from "~/components/Dialog";
import { type Stage, requiresHandle } from "./service-config";
import { LinkAccountContext } from "./link-account-context";
import { HandleStage, LoadingStage, RedirectStage, DoneStage, ErrorStage, ConfigureStage, ApplyStage } from "./LinkAccountStages";

export const OAUTH_CHANNEL = "a0eb0210-bc9a-4bc5-be15-44ff49b71027";

export default function LinkAccountDialog({ onClose, service, initialCaps, existingConnectionId, currentEnabledCaps }: {
    onClose: () => void;
    service: string;
    initialCaps?: number;
    existingConnectionId?: string;
    currentEnabledCaps?: number;
}) {
    const handle = useSignal("");
    const redirectUrl = useSignal("");
    const stage = useSignal<Stage>(existingConnectionId ? 'configure' : requiresHandle(service) ? 'handle' : 'loading');
    const error = useSignal<string | null>(null);
    const connectionId = useSignal(existingConnectionId ?? "");

    useLayoutEffect(() => {
        if (existingConnectionId) {
            connectionId.value = existingConnectionId;
            stage.value = 'configure';
        } else {
            stage.value = requiresHandle(service) ? 'handle' : 'loading';
        }
    }, [service, existingConnectionId]);

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
        <LinkAccountContext.Provider value={{ handle, redirectUrl, stage, error, connectionId, service, initialCaps, currentEnabledCaps, onClose }}>
            <Dialog class="link-account-dialog" onClose={onClose}>
                {renderStage()}
            </Dialog>
        </LinkAccountContext.Provider>
    );
}
