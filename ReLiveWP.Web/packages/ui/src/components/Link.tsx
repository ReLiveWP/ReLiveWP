import type { ComponentChildren } from "preact";
import { useLocation } from "preact-iso";

type LinkProps = {
    class?: string | undefined;
    activeClass?: string | undefined;
    href: string;
    children: ComponentChildren;
    exactMatch?: boolean | undefined;
};

export default function Link({
    class: inactive,
    activeClass,
    exactMatch,
    ...props
}: LinkProps) {
    const active = [inactive, activeClass].filter(Boolean).join(" ");
    const path = useLocation().path as string | undefined;
    const matches = path !== undefined
        && (exactMatch === true
            ? path === props.href
            : path === props.href || path.startsWith(`${props.href}/`));

    return <a {...props} class={matches ? active : inactive} />;
}
