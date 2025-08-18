import "./nav-header.scss"


import Link from "./Link";
import NavLoginLink from "./NavLoginLink";
import NavLogo from "./NavLogo";

export default function NavHeader() {
    return (
        <header>
            <div class="brand-container">
                <a class="home-link" href="/">
                    <NavLogo class="brand-logo" />
                    <div class="header-container">
                        <h1 class="header-title">ReLive</h1>
                        <p class="header-subtitle text-accent">for Windows Phone</p>
                    </div>
                </a>
            </div>
            <nav class="header-nav">
                <ul class="header-items leading">
                    <li>
                        <Link activeClass="active text-accent" exactMatch href="/">discover</Link>
                    </li>
                    <li>
                        <Link activeClass="active text-accent" href="/downloads">download</Link>
                    </li>
                    <li>
                        <Link activeClass="active text-accent" href="/marketplace">marketplace</Link>
                    </li>
                    <li>
                        <Link activeClass="active text-accent" href="/help">how-to</Link>
                    </li>
                    <li>
                        <Link activeClass="active text-accent" href="/my/device">my phone</Link>
                    </li>
                </ul>
                <ul class="header-items trailing">
                    <li>
                        <NavLoginLink />
                    </li>
                </ul>
            </nav>
            <hr class="header-splitter"></hr>
        </header>
    );
}