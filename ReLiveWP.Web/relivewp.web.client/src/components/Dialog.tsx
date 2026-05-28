import { ComponentChildren } from "preact";
import { useEffect, useRef } from "preact/hooks";

export function Dialog({ onClose, class: className, children }: {
    onClose: () => void;
    class?: string;
    children: ComponentChildren;
}) {
    const ref = useRef<HTMLDialogElement>(null);

    useEffect(() => {
        ref.current?.showModal();
    }, []);

    return (
        <dialog ref={ref} class={className} onClose={onClose}>
            {children}
        </dialog>
    );
}
