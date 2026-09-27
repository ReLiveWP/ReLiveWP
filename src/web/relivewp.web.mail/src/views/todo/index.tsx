import { useTitle } from "@relivewp/ui";

import Page from "~/components/Page";

export default function Todo() {
    useTitle("to-do");

    return (
        <Page title="to-do">
            <p class="note">Nothing here yet.</p>
        </Page>
    );
}
