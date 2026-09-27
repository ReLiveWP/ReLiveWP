import { ComponentChildren } from "preact";

export const ActionLink = ({ class: className = "text-accent", onClick, children }: {
    class?: string;
    onClick: () => void;
    children: ComponentChildren;
}) => (
    <a href="#" class={className} onClick={(e) => {
        e.preventDefault();
        onClick();
    }}>{children} &gt;</a>
);
