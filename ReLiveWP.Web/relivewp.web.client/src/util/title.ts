import { useEffect } from "preact/hooks";

export function useTitle(title: string) {
    return useEffect(() => {
        document.title = !!title ? `${title} - relive for windows phone` : 'relive for windows phone'
    });
}