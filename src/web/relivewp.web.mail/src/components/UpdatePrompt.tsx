import "./update-prompt.scss";

import { applyUpdate, updateReady } from "~/util/service-worker";

export default function UpdatePrompt() {
    if (!updateReady.value) return null;

    return (
        <button type="button" class="update-prompt text-accent" onClick={applyUpdate}>
            update ready, reload
        </button>
    );
}
