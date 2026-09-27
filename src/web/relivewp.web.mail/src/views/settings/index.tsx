import { useTitle } from "@relivewp/ui";

import Page from "~/components/Page";
import { useAppState } from "~/state/app-state";

export default function Settings() {
    useTitle("settings");

    const { signOut } = useAppState();

    return (
        <Page title="settings">
            <button type="button" onClick={signOut}>sign out</button>
        </Page>
    );
}
