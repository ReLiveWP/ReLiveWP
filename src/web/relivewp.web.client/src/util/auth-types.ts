export type Visibility = "private" | "mutual" | "public";

export type User = {
    id: string,
    cid: string,
    puid: string,
    username: string,
    email_address: string,
    first_name: string | null,
    last_name: string | null,
    picture_url: string | null,
    picture_etag: string | null,
    visibility: Visibility
}
