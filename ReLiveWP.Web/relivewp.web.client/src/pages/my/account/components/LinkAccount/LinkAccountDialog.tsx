import "../link-account-dialog.scss";

import { useSignal } from "@preact/signals";
import { useEffect, useLayoutEffect } from "preact/hooks";

import { Dialog } from "@relivewp/ui";
import { type Stage, stageForService } from "./service-config";
import { LinkAccountContext } from "./link-account-context";
import { ServiceStage, HandleStage, CredentialsStage, LoadingStage, RedirectStage, DoneStage, ErrorStage, ConfigureStage, ApplyStage } from "./LinkAccountStages";

function initialStage(service?: string): Stage {
    if (!service) return 'service';
    return stageForService(service);
}

export { OAUTH_CHANNEL } from "~/util/oauth";
import { OAUTH_CHANNEL } from "~/util/oauth";

export default function LinkAccountDialog({ onClose, service: initialService, initialCaps, existingConnectionId, currentEnabledCaps, relinkConnectionId }: {
    onClose: () => void;
    service?: string;
    initialCaps?: number;
    existingConnectionId?: string;
    currentEnabledCaps?: number;
    relinkConnectionId?: string;
}) {
    const handle = useSignal("");
    const redirectUrl = useSignal("");
    const service = useSignal(initialService ?? "");
    const stage = useSignal<Stage>(
        existingConnectionId ? 'configure' :
        relinkConnectionId ? 'credentials' :
        initialStage(initialService));
    const error = useSignal<string | null>(null);
    const connectionId = useSignal(existingConnectionId ?? relinkConnectionId ?? "");

    useLayoutEffect(() => {
        service.value = initialService ?? "";
        if (existingConnectionId) {
            connectionId.value = existingConnectionId;
            stage.value = 'configure';
        } else if (relinkConnectionId) {
            connectionId.value = relinkConnectionId;
            stage.value = 'credentials';
        } else {
            stage.value = initialStage(initialService);
        }
    }, [initialService, existingConnectionId, relinkConnectionId]);

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
            case 'service': return <ServiceStage />;
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
