export type User = {
    id: string,
    cid: string,
    puid: string,
    username: string,
    email_address: string
}

export type SecurityToken = {
    service_target: string,
    token: string,
    token_type: string,
    created: string,
    expires: string
}

export type SecurityTokensResponse = {
    puid: number,
    cid: string,
    username: string,
    email_address: string,
    security_tokens: SecurityToken[]
}
