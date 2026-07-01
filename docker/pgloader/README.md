# SQLite → PostgreSQL data migration

One-time copy of the legacy per-service SQLite databases into PostgreSQL. Schema is owned by EF
Core (the `InitialPostgres` migrations, applied on service startup); pgloader only moves rows.

## Order of operations

1. **Start Postgres** and let it create the six databases:
   ```sh
   docker compose up -d postgres
   ```
2. **Create the schema** — bring the data services up once so `Database.Migrate()` runs, or apply
   migrations however you prefer. pgloader does a *data-only* load and needs the tables to exist.
3. **Copy the data:**
   ```sh
   docker/pgloader-migrate.sh            # all services
   docker/pgloader-migrate.sh identity   # or one at a time
   ```
4. **Verify** row counts match the old SQLite files:
   ```sh
   docker compose exec postgres psql -U relive -d relive_identity -c '\dt+'
   ```

## Notes

- `*.load` files do a `data only, truncate, reset sequences` load, so they are safe to re-run.
- **Push is intentionally partial:** only the `Channels` table is copied. `Sessions` and
  `Notifications` are transient (regenerated on device reconnect) and are headed for Redis in a
  later phase, so their rows are skipped.
- Owned-type columns (`LastLocation_*` on Skybox devices, the connected-service profile) and the
  ASP.NET Identity tables map 1:1 because both the SQLite and Postgres schemas were generated from
  the same EF model. If you renamed a property after the original SQLite migration, check the
  column names line up before loading.
- Credentials in the `.load` files default to `relive:relive`. If you changed the Postgres
  password (`deploy/secrets/postgres_password`), update the connection strings in the `.load`
  files to match.
