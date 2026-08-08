import "../link-account-dialog.scss";

import { useSignal } from "@preact/signals";
import { useEffect, useLayoutEffect } from "preact/hooks";

import { Dialog } from "~/components/Dialog";
import { type Stage, requiresCredentials, requiresHandle } from "./service-config";
import { LinkAccountContext } from "./link-account-context";
import { HandleStage, CredentialsStage, LoadingStage, RedirectStage, DoneStage, ErrorStage, ConfigureStage, ApplyStage } from "./LinkAccountStages";

function initialStage(service: string): Stage {
    if (requiresCredentials(service)) return 'credentials';
    return requiresHandle(service) ? 'handle' : 'loading';
}

export { OAUTH_CHANNEL } from "~/util/oauth";
import { OAUTH_CHANNEL } from "~/util/oauth";

export default function LinkAccountDialog({ onClose, service, initialCaps, existingConnectionId, currentEnabledCaps, relinkConnectionId }: {
    onClose: () => void;
    service: string;
    initialCaps?: number;
    existingConnectionId?: string;
    currentEnabledCaps?: number;
    relinkConnectionId?: string;
}) {
    const handle = useSignal("");
    const redirectUrl = useSignal("");
    const stage = useSignal<Stage>(
        existingConnectionId ? 'configure' :
        relinkConnectionId ? 'credentials' :
        initialStage(service));
    const error = useSignal<string | null>(null);
    const connectionId = useSignal(existingConnectionId ?? relinkConnectionId ?? "");

    useLayoutEffect(() => {
        if (existingConnectionId) {
            connectionId.value = existingConnectionId;
            stage.value = 'configure';
        } else if (relinkConnectionId) {
            connectionId.value = relinkConnectionId;
            stage.value = 'credentials';
        } else {
            stage.value = initialStage(service);
        }
    }, [service, existingConnectionId, relinkConnectionId]);

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
            case 'credentials': return <CredentialsStage />;
            case 'loading': return <LoadingStage />;
            case 'redirect': return <RedirectStage />;
            case "configure": return <ConfigureStage />;
            case "applying": return <ApplyStage/>
            case 'done': return <DoneStage />;
            case 'error': return <ErrorStage />;
        }
    };

    return (
        <LinkAccountContext.Provider value={{ handle, redirectUrl, stage, error, connectionId, service, initialCaps, currentEnabledCaps, isRelink: !!relinkConnectionId, onClose }}>
            <Dialog class="link-account-dialog" onClose={onClose}>
                {renderStage()}
            </Dialog>
        </LinkAccountContext.Provider>
    );
}
