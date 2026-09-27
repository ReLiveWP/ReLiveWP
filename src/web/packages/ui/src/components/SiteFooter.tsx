import type { ComponentChildren } from "preact";

export function SiteFooter({ children }: { children?: ComponentChildren }) {
    return (
        <footer>
            <div class="site-footer">
                {children}
                <p>relive for windows phone &bull; windows phone is a trademark of Microsoft Corp.</p>
            </div>
        </footer>
    );
}
