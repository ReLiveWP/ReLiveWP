import { useSignal } from "@preact/signals";
import { useCallback } from "preact/hooks";

import { useAppState, useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_UPDATE_PROFILE } from "~/util/endpoints";
import type { Visibility } from "~/util/auth-types";

import "./privacy.scss";

const OPTIONS: { value: Visibility, name: string, hint: string }[] = [
    {
        value: "private",
        name: "private",
        hint: "nobody gets linked to your profile automatically. people can still link your profile manually.",
    },
    {
        value: "mutual",
        name: "mutual",
        hint: "people who have your email saved get linked to your profile, but only if you have them saved too.",
    },
    {
        value: "public",
        name: "public",
        hint: "anyone who has your email saved gets linked to your profile.",
    },
];

export default function Privacy() {
    const { user, refreshUser } = useAppState();
    const fetch = useAuthenticatedFetch();

    const busy = useSignal(false);
    const error = useSignal<string | null>(null);

    const save = useCallback(async (visibility: Visibility) => {
        busy.value = true;
        error.value = null;

        try {
            const response = await fetch(ENDPOINT_UPDATE_PROFILE, {
                method: 'PATCH',
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ visibility }),
            });

            if (!response.ok) {
                error.value = "something went wrong, try again.";
                return;
            }

            await refreshUser();
        }
        finally {
            busy.value = false;
        }
    }, [fetch, refreshUser]);

    if (!user.value) return null;

    const current = user.value.visibility;

    return (
        <div class={"privacy" + (busy.value ? " disabled" : "")}>
            <h4>Discovery</h4>
            <p class="intro">
                These options determine your profile's visibility to other ReLive users who have your email address in their address book. 
                If enabled, ReLive can automatically link your profile with their contact, and enable social features automatically!
            </p>
            <p class="intro">
                <b>No contact details are ever shared</b>, but other ReLive users will know you exist, your status on Messenger, and can see 
                any public info, including your profile picture. 
            </p>

            {OPTIONS.map(option => (
                <label class="visibility-option" key={option.value}>
                    <input type="radio" name="visibility" value={option.value}
                        checked={current === option.value}
                        onChange={() => save(option.value)} />
                    <span class="visibility-name">{option.name}</span>
                    <span class="visibility-hint">{option.hint}</span>
                </label>
            ))}

            {error.value && <p class="error">{error.value}</p>}
        </div>
    );
}
