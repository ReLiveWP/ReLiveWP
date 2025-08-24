import { ComponentChildren, RenderableProps } from 'preact';

import { useLocation } from 'preact-iso'

type LinkProps = {
    class?: string
    activeClass?: string
    href: string
    children: ComponentChildren
    exactMatch?: boolean
};

export default function Link({
    class: inactive,
    activeClass,
    exactMatch,
    ...props
}: LinkProps) {
    const active = [inactive, activeClass].filter(Boolean).join(' ');
    const url = useLocation().url;
    // const matches = (url === '/' && props.href === '/') || props.href !== '/' && url.startsWith(props.href);
    const matches = exactMatch ? (url === props.href) : url.startsWith(props.href);

    return <a {...props} class={matches ? active : inactive} />;
}