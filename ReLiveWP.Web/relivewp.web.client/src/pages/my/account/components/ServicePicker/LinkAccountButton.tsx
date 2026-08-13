import { useOpenDialog } from "../../state/linked-accounts";

export default function LinkAccountButton() {
    const openDialog = useOpenDialog();

    return (
        <a href="#" class="action-link text-accent" onClick={() => openDialog({ dialog: 'link' })}>
            add another account &gt;
        </a>
    );
}
