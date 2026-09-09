type Props = {
    class: string,
    url: string | undefined,
    initial: string,
    onError?: () => void,
};

export default function Tile({ class: className, url, initial, onError }: Props) {
    if (url === undefined) return <span class={className}>{initial}</span>;

    return <img class={className} src={url} alt="" loading="lazy" decoding="async" onError={onError} />;
}
