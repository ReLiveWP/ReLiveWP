import "./nav-header.scss"

import ReLiveLogoSmall from "../../static/brand/relivewp-small.svg"
import Link from "./Link";
import NavLoginLink from "./NavLoginLink";

export default function NavHeader() {

    return (
        <header>
            <div class="brand-container">
                <a class="home-link" href="/">
                    <img class="brand-logo" src={ReLiveLogoSmall} />
                    <div class="header-container">
                        <h1 class="header-title">ReLive</h1>
                        <p class="header-subtitle">for Windows Phone</p>
                    </div>
                </a>
            </div>
            <nav class="header-nav">
                <ul class="header-items leading">
                    <li>
                        <Link activeClass="active" href="/">discover</Link>
                    </li>
                    <li>
                        <Link activeClass="active" href="/downloads">download</Link>
                    </li>
                    <li>
                        <Link activeClass="active" href="/marketplace">marketplace</Link>
                    </li>
                    <li>
                        <Link activeClass="active" href="/help">how-to</Link>
                    </li>
                    <li>
                        <Link activeClass="active" href="/my">my phone</Link>
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