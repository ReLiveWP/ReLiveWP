import { photoOf, type Aggregate } from "../state/aggregate";
import ContactName from "./ContactName";
import { initialOf } from "../state/groups";

type Props = {
    favourites: Aggregate[],
    total: number,
    photos: ReadonlyMap<string, string>,
    onSelect: (aggregate: Aggregate) => void,
};

export default function Favourites({ favourites, total, photos, onSelect }: Props) {
    return (
        <section class="favourites">
            <div class="panel-head">
                <h2>favourites</h2>
                {favourites.length > 0 && <span class="panel-count">{favourites.length} of {total}</span>}
            </div>

            {favourites.length === 0
                ? <p class="note">No favourites yet. Pin someone on your phone and they turn up here.</p>
                : (
                    <div class="favourite-grid">
                        {favourites.map((aggregate) => {
                            const url = photoOf(aggregate, photos);

                            return (
                                <button
                                    key={aggregate.key}
                                    type="button"
                                    class="favourite-tile"
                                    onClick={() => { onSelect(aggregate); }}
                                >
                                    {url === undefined
                                        ? <span class="favourite-initial">{initialOf(aggregate.primary)}</span>
                                        : <img src={url} alt="" loading="lazy" decoding="async" />}
                                    <span class="favourite-caption"><ContactName contact={aggregate.primary} /></span>
                                </button>
                            );
                        })}
                    </div>
                )}
        </section>
    );
}
