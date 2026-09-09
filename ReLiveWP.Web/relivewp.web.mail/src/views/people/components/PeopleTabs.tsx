import type { ComponentChildren } from "preact";

export type PeopleTab = "feed" | "albums";

type Props = {
    active: PeopleTab,
    hrefFor: (tab: PeopleTab) => string,
};

function TabLink({ tab, active, hrefFor, children }: Props & { tab: PeopleTab, children: ComponentChildren }) {
    return <a class={tab === active ? "on" : undefined} href={hrefFor(tab)}>{children}</a>;
}

export default function PeopleTabs({ active, hrefFor }: Props) {
    return (
        <nav class="panel-tabs">
            <TabLink tab="feed" active={active} hrefFor={hrefFor}>what&rsquo;s new</TabLink>
            <TabLink tab="albums" active={active} hrefFor={hrefFor}>photos</TabLink>
        </nav>
    );
}
