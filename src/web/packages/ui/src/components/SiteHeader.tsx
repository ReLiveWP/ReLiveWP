import type { ComponentChildren } from "preact";

import BrandLogo from "./BrandLogo.tsx";
import Link from "./Link.tsx";
import { SearchBox, type SearchProps } from "./SearchBox.tsx";

export type NavItem = {
    href: string;
    label: string;
    exact?: boolean;
};

function navItem(item: NavItem) {
    return (
        <li key={item.href}>
            <Link activeClass="active text-accent" exactMatch={item.exact} href={item.href}>
                {item.label}
            </Link>
        </li>
    );
}

export function SiteHeader({
    items = [],
    trailing = [],
    account,
    search,
    home = "/",
}: {
    items?: NavItem[];
    trailing?: NavItem[];
    account?: ComponentChildren;
    search?: SearchProps;
    home?: string;
}) {
    return (
        <header>
            <div class="brand-container">
                {search !== undefined ? (
                    <div class="brand-search">
                        <SearchBox {...search} />
                    </div>
                ) : null}
                <a class="home-link" href={home}>
                    <BrandLogo class="brand-logo" />
                    <div class="header-container">
                        <h1 class="header-title">ReLive</h1>
                        <p class="header-subtitle text-accent">for Windows Phone</p>
                    </div>
                </a>
            </div>
            <nav class="header-nav">
                <ul class="header-items leading">{items.map(navItem)}</ul>
                {trailing.length > 0 || account !== undefined ? (
                    <ul class="header-items trailing">
                        {trailing.map(navItem)}
                        {account !== undefined ? <li>{account}</li> : null}
                    </ul>
                ) : null}
            </nav>
            <hr class="header-splitter" />
        </header>
    );
}
