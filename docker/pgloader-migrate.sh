#!/usr/bin/env bash
#
# One-time SQLite -> PostgreSQL data copy via pgloader.
#
# Prerequisites:
#   1. The postgres compose service is up:            docker compose up -d postgres
#   2. Each service has run once so EF created its schema (docker compose up identity ... ),
#      OR apply migrations however you normally do. pgloader does a data-only load and will
#      fail if the target tables don't exist yet.
#   3. The legacy .db files live in deploy/data/<service>/ (with any -wal/-shm sidecars).
#
# Usage:   docker/pgloader-migrate.sh [service ...]
#          (no args = all services)
#
# The service data directory is mounted read-write so SQLite can replay any WAL sidecar on open.
# After each load we realign identity sequences with the copied rows: pgloader's own
# "reset sequences" is a no-op in data-only mode, so without this the next insert collides.
# Credentials default to relive:relive — if you changed the postgres password, update
# docker/pgloader/*.load and PGPW below to match.
#
# On Windows run under Git Bash with MSYS path translation disabled so container paths survive:
#   MSYS_NO_PATHCONV=1 MSYS2_ARG_CONV_EXCL='*' docker/pgloader-migrate.sh
set -euo pipefail
cd "$(dirname "$0")/.."

NETWORK="${PG_NETWORK:-relivewp_relive}"   # docker compose default: <project>_<network>
IMAGE="${PGLOADER_IMAGE:-dimitri/pgloader:latest}"
PG_IMAGE="${PG_IMAGE:-postgres:17-alpine}"
PGPW="${PGPW:-relive}"

# service -> filename of its legacy sqlite db inside deploy/data/<service>/
declare -A DBFILE=(
  [identity]="users.db"
  [connectedservices]="connectedservices.db"
  [deviceregistration]="devices.db"
  [skybox]="sky.db"
  [skydrive]="skydrive.db"
  [push]="push.db"
)

# realign every owned identity sequence with max(column) for all tables in the current db
read -r -d '' RESET_SEQUENCES_SQL <<'SQL' || true
DO $$
DECLARE r record; m bigint;
BEGIN
  FOR r IN
    SELECT c.table_name, c.column_name,
           pg_get_serial_sequence(quote_ident(c.table_name), c.column_name) AS seq
    FROM information_schema.columns c
    WHERE c.table_schema = 'public'
      AND pg_get_serial_sequence(quote_ident(c.table_name), c.column_name) IS NOT NULL
  LOOP
    EXECUTE format('SELECT max(%I) FROM %I', r.column_name, r.table_name) INTO m;
    IF m IS NOT NULL THEN PERFORM setval(r.seq, m); END IF;
  END LOOP;
END $$;
SQL

# docker on Windows/Git-Bash wants a native path for the bind-mount source
hostpath() { command -v cygpath >/dev/null 2>&1 && cygpath -w "$1" || printf '%s' "$1"; }

services=("$@")
[ ${#services[@]} -eq 0 ] && services=(identity connectedservices deviceregistration skybox skydrive push)

for svc in "${services[@]}"; do
  db="${DBFILE[$svc]:-}"
  dir="deploy/data/$svc"
  load="docker/pgloader/${svc}.load"
  if [ -z "$db" ]; then echo "!! unknown service '$svc', skipping"; continue; fi
  if [ ! -f "$dir/$db" ]; then echo "-- $svc: no legacy db at $dir/$db, skipping"; continue; fi

  echo "== $svc: $dir/$db -> postgres/relive_${svc} =="
  docker run --rm --network "$NETWORK" \
    -v "$(hostpath "$(pwd)/$dir"):/data" \
    -v "$(hostpath "$(pwd)/$load"):/load.load:ro" \
    "$IMAGE" pgloader /load.load

  echo "-- realigning identity sequences for relive_${svc}"
  docker run --rm --network "$NETWORK" -e PGPASSWORD="$PGPW" "$PG_IMAGE" \
    psql -h postgres -U relive -d "relive_${svc}" -v ON_ERROR_STOP=1 -c "$RESET_SEQUENCES_SQL"
done

echo "done. verify row counts with:  docker compose exec postgres psql -U relive -d relive_<svc> -c '\\dt+'"
