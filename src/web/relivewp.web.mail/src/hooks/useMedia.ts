import { useEffect, useState } from "preact/hooks";

export function useMedia(query: string): boolean {
    const [matches, setMatches] = useState(() => matchMedia(query).matches);

    useEffect(() => {
        const list = matchMedia(query);
        const update = () => { setMatches(list.matches); };

        update();
        list.addEventListener("change", update);

        return () => { list.removeEventListener("change", update); };
    }, [query]);

    return matches;
}
