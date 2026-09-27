import type { ComponentChildren } from "preact";
import { useEffect, useRef } from "preact/hooks";

type DialogPolyfill = { registerDialog(dialog: HTMLDialogElement): void };

export function Dialog({ onClose, class: className, children }: {
    onClose: () => void;
    class?: string;
    children: ComponentChildren;
}) {
    const ref = useRef<HTMLDialogElement>(null);

    useEffect(() => {
        const dialog = ref.current;
        if (!dialog) return;

        if (dialog.showModal) {
            dialog.showModal();
            return;
        }

        let closed = false;
        import("dialog-polyfill").then((module) => {
            if (closed) return;

            const polyfill = module.default as unknown as DialogPolyfill;
            polyfill.registerDialog(dialog);
            dialog.setAttribute("data-polyfilled", "");
            dialog.showModal();
        });

        return () => { closed = true; };
    }, []);

    return (
        <dialog ref={ref} class={className} onClose={onClose}>
            {children}
        </dialog>
    );
}
