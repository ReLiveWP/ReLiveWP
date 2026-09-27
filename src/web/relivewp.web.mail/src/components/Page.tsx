import "./page.scss";

import type { ComponentChildren } from "preact";

import SyncStatus from "./SyncStatus";
import UpdatePrompt from "./UpdatePrompt";

type Props = {
    title: ComponentChildren,
    subtitle?: ComponentChildren,
    actions?: ComponentChildren,
    sidebar?: ComponentChildren,
    detail?: ComponentChildren,
    class?: string,
    children?: ComponentChildren,
};

export default function Page(
    { title, subtitle, actions, sidebar, detail, class: extra, children }: Props,
) {
    return (
        <section class={extra === undefined ? "page" : `page ${extra}`}>
            <div class="page-bar">
                <div class="page-heading">
                    <h2>{title}</h2>
                    {subtitle !== undefined && <p class="page-subtitle">{subtitle}</p>}
                </div>

                {actions !== undefined && <div class="page-actions">{actions}</div>}
            </div>

            <div class="page-body">
                <aside class="page-sidebar">
                    <div class="page-sidebar-main">{sidebar}</div>

                    <div class="page-sidebar-foot">
                        <UpdatePrompt />
                        <SyncStatus />
                    </div>
                </aside>

                <div class="page-content">{children}</div>

                {detail !== undefined && <div class="page-detail">{detail}</div>}
            </div>
        </section>
    );
}
