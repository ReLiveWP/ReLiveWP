export default function WhatsNew() {
    return (
        <section class="whats-new">
            <div class="panel-head">
                <h2>what&rsquo;s new</h2>
            </div>

            {/* the People hub feed is server side and has no client source yet, so this stays
                an empty state rather than inventing one */}
            <p class="note">
                Nothing here yet. Connected accounts will post their updates here.
            </p>
        </section>
    );
}
