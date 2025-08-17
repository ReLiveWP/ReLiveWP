import { ComponentChildren, RenderableProps } from 'preact';
import { useLocation } from 'preact-iso'

type LinkProps = {
    class?: string
    activeClass?: string
    href: string
    children: ComponentChildren
};

export default function Link({
    class: inactive,
    activeClass,
    ...props
}: LinkProps) {
    const active = [inactive, activeClass].filter(Boolean).join(' ');
    const matches = useLocation().url === props.href;

    return <a {...props} class={matches ? active : inactive} />;
}