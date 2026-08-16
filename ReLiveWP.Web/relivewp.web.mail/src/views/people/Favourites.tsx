import type { Contact } from "@relivewp/eas-store";

import ContactName from "./ContactName";
import { initialOf } from "./groups";

type Props = {
    favourites: Contact[],
    total: number,
    photos: ReadonlyMap<string, string>,
    onSelect: (contact: Contact) => void,
};

export default function Favourites({ favourites, total, photos, onSelect }: Props) {
    return (
        <section class="favourites">
            <div class="panel-head">
                <h2>favourites</h2>
                {favourites.length > 0 && <span class="panel-count">{favourites.length} of {total}</span>}
            </div>

            {favourites.length === 0
                ? <p class="note">No favourites yet. Star someone on your phone and they turn up here.</p>
                : (
                    <div class="favourite-grid">
                        {favourites.map((contact) => {
                            const url = photos.get(contact.id);

                            return (
                                <button
                                    key={contact.id}
                                    type="button"
                                    class="favourite-tile"
                                    onClick={() => { onSelect(contact); }}
                                >
                                    {url === undefined
                                        ? <span class="favourite-initial">{initialOf(contact)}</span>
                                        : <img src={url} alt="" loading="lazy" decoding="async" />}
                                    <span class="favourite-caption"><ContactName contact={contact} /></span>
                                </button>
                            );
                        })}
                    </div>
                )}
        </section>
    );
}
