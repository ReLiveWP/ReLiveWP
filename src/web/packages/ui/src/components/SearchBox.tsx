export type SearchProps = {
    value?: string;
    placeholder?: string;
    name?: string;
    action?: string;
    submitLabel?: string;
    onInput?: (value: string) => void;
    onSubmit?: (value: string) => void;
};

export function SearchBox({
    value,
    placeholder = "search",
    name = "q",
    action,
    submitLabel = "Search",
    onInput,
    onSubmit,
}: SearchProps) {
    const handled = onSubmit !== undefined || onInput !== undefined;

    return (
        <form
            class="header-search"
            action={action}
            method="get"
            onSubmit={(event) => {
                if (!handled) return;

                event.preventDefault();
                onSubmit?.(new FormData(event.currentTarget).get(name)?.toString() ?? "");
            }}
        >
            <input
                type="search"
                name={name}
                value={value}
                placeholder={placeholder}
                onInput={(event) => onInput?.(event.currentTarget.value)}
            />
            <input type="submit" value={submitLabel} />
        </form>
    );
}
