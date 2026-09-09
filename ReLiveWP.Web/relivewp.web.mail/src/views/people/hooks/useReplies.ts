import { useCallback, useState } from "preact/hooks";

import { useAsync } from "~/hooks/useAsync";
import { useAppState } from "~/state/app-state";
import { reason } from "~/util/reason";
import { fetchRepliesAsync, postReplyAsync, SocialError, type SocialEntry } from "../state/social";

export type RepliesState = {
    entries: SocialEntry[],
    loading: boolean,
    error: string | null,
    posting: boolean,
    postError: string | null,
};

const NONE: SocialEntry[] = [];

function replyReason(thrown: unknown): string {
    if (thrown instanceof SocialError && thrown.code === "not_connected")
        return "link an account to reply.";

    return reason(thrown);
}

function localReply(text: string): SocialEntry {
    return {
        id: `local:${Date.now()}`,
        provider_id: "local",
        type: "post",
        published: new Date().toISOString(),
        title: "Reply",
        content: text,
        generator: "",
        canonical_url: "",
        categories: [],
        can_reply: false,
        reply_count: null,
        author: {
            cid: null, is_me: true, provider: "", external_id: "",
            display_name: "you", screen_name: "", avatar_url: "", canonical_url: "",
        },
        photos: [],
    };
}

export function useReplies(activityId: string | null): RepliesState & { post: (text: string) => Promise<boolean> } {
    const fetcher = useAppState().authenticatedFetch.value;
    const [posting, setPosting] = useState(false);
    const [postError, setPostError] = useState<string | null>(null);

    const replies = useAsync(
        fetcher === undefined || activityId === null
            ? null
            : async () => {
                const page = await fetchRepliesAsync(fetcher, activityId);
                return page.entries;
            },
        [fetcher, activityId],
        { describe: replyReason });

    const post = useCallback(async (text: string): Promise<boolean> => {
        if (fetcher === undefined || activityId === null) return false;

        setPosting(true);
        setPostError(null);

        try {
            await postReplyAsync(fetcher, activityId, text);
        }
        catch (thrown: unknown) {
            setPosting(false);
            setPostError(replyReason(thrown));
            return false;
        }

        setPosting(false);
        replies.set((prev) => [...(prev ?? NONE), localReply(text)]);
        return true;
    }, [fetcher, activityId, replies.set]);

    return { entries: replies.value ?? NONE, loading: replies.loading, error: replies.error, posting, postError, post };
}
