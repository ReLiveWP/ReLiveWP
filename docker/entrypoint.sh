#!/bin/sh
set -e

TRUST_DIR=/usr/local/share/ca-certificates/relive

# Inject the public root CA(s) bind-mounted at $TRUST_DIR into the OS trust store.
if [ -d "$TRUST_DIR" ] && ls "$TRUST_DIR"/*.crt >/dev/null 2>&1; then
    update-ca-certificates >/dev/null 2>&1 || echo "warning: update-ca-certificates failed" >&2
fi

# Bind mounts come in owned by the host user; hand persisted state to the app user.
if [ -n "${APP_UID}" ]; then
    [ -d "${HOME}/.dotnet" ] && chown -R "${APP_UID}" "${HOME}/.dotnet" 2>/dev/null || true
    [ -d /app/data ] && chown -R "${APP_UID}" /app/data 2>/dev/null || true
fi

# SERVICE_DLL is baked into the image (ENV SERVICE_DLL=<Project>.dll).
exec gosu "${APP_UID:-app}" dotnet "${SERVICE_DLL}" "$@"
