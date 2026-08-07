import { ComponentChildren } from "preact";
import { useState } from "preact/hooks";

import { OpenDialogAction, OpenDialogContext } from "../state/linked-accounts";
import LinkAccountDialog from "./LinkAccount/LinkAccountDialog";
import RelinkAccountDialog from "./RelinkAccount/RelinkAccountDialog";
import UnlinkAccountDialog from "./UnlinkAccount/UnlinkAccountDialog";

export function Dialogs({ children }: { children: ComponentChildren }) {
    const [activeDialog, setActiveDialog] = useState<OpenDialogAction | null>(null);
    const closeDialog = () => setActiveDialog(null);

    const renderDialog = () => {
        if (!activeDialog) return null;
        switch (activeDialog.dialog) {
            case 'link':
                return (
                    <LinkAccountDialog 
                        service={activeDialog.service} 
                        initialCaps={activeDialog.initialCaps} 
                        existingConnectionId={activeDialog.existingConnectionId} 
                        currentEnabledCaps={activeDialog.currentEnabledCaps} 
                        onClose={closeDialog} />
                );
            case 'relink':
                return <RelinkAccountDialog id={activeDialog.id} onClose={closeDialog} />;
            case 'unlink':
                return <UnlinkAccountDialog service={activeDialog.service} id={activeDialog.id} onClose={closeDialog} />;
        }
    };

    return (
        <OpenDialogContext.Provider value={setActiveDialog}>
            {renderDialog()}
            {children}
        </OpenDialogContext.Provider>
    );
}
