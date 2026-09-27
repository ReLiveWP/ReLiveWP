import { AvatarCropDialog, type AvatarCrop } from "@relivewp/ui";
import { lazy } from "preact-iso";
import { useCallback, useRef, useState } from "preact/hooks";

import { useAppState, useAuthenticatedFetch } from "~/state/app-state";
import { ENDPOINT_USER_PICTURE } from "~/util/endpoints";

import "./components/avatar.scss";

const EditNameDialog = lazy(() => import("./components/EditNameDialog"));

export default function AccountDetails() {
    const { user, refreshUser } = useAppState();
    const fetch = useAuthenticatedFetch();

    const picker = useRef<HTMLInputElement>(null);
    const [cropping, setCropping] = useState<File | null>(null);
    const [editingName, setEditingName] = useState(false);

    const uploadPicture = useCallback(async (file: File, crop: AvatarCrop) => {
        const body = new FormData();
        body.append("file", file);
        body.append("crop_x", String(crop.x));
        body.append("crop_y", String(crop.y));
        body.append("crop_size", String(crop.size));

        const response = await fetch(ENDPOINT_USER_PICTURE, { method: 'PUT', body });
        if (!response.ok)
            throw new Error("that image could not be used, try another.");

        await refreshUser();
    }, [fetch, refreshUser]);

    const removePicture = useCallback(async () => {
        await fetch(ENDPOINT_USER_PICTURE, { method: 'DELETE' });
        await refreshUser();
    }, [fetch, refreshUser]);

    // the token is in hand before the /auth/user/@me round trip finishes, so the route renders first
    if (!user.value) return null;

    const fullName = [user.value.first_name, user.value.last_name].filter(Boolean).join(" ");

    return (
        <>
            {cropping && (
                <AvatarCropDialog
                    file={cropping}
                    onConfirm={uploadPicture}
                    onClose={() => setCropping(null)} />
            )}
            {editingName && <EditNameDialog onClose={() => setEditingName(false)} />}

            <h4>Picture</h4>
            <div class="account-avatar">
                {user.value.picture_url
                    ? <img class="account-avatar-tile"
                        src={`${user.value.picture_url}?v=${user.value.picture_etag}`}
                        alt="your profile picture" />
                    : <div class="account-avatar-tile">{(fullName || user.value.username || "?")[0]}</div>}

                <div class="account-avatar-actions">
                    <input ref={picker} type="file" accept="image/*"
                        onChange={e => {
                            const input = e.currentTarget as HTMLInputElement;
                            const file = input.files?.[0];
                            if (file) setCropping(file);
                            input.value = "";
                        }} />
                    <button onClick={() => picker.current?.click()}>change</button>
                    {user.value.picture_url && <button onClick={() => removePicture()}>remove</button>}
                </div>
            </div>

            <h4>Information</h4>
            <dl>
                <dt>id</dt>
                <dd>{user.value.id}</dd>
                <dd>{user.value.cid}</dd>
                <dd>{user.value.puid}</dd>
                <dt>name</dt>
                <dd>{fullName || "not set"} <button onClick={() => setEditingName(true)}>change</button></dd>
                <dt>username</dt>
                <dd>{user.value.username} <button>change</button></dd>
                <dt>email address</dt>
                <dd>{user.value.email_address}</dd>
                <dt>password</dt>
                <dd>************ <button>change</button></dd>
            </dl>
        </>
    );
}
