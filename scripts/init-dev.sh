#!/usr/bin/env bash
# Initialises secrets required for local development and Docker Compose.
# Safe to re-run: existing secrets are never overwritten.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SECRETS_DIR="$ROOT_DIR/deploy/secrets"
IDENTITY_DIR="$ROOT_DIR/ReLiveWP.Backend.Identity"

mkdir -p "$SECRETS_DIR"

# ── JWT ECDSA P-256 key pair ──────────────────────────────────────────────────
if [ -f "$SECRETS_DIR/jwt_private_key" ]; then
    echo "JWT private key already exists, skipping generation."
else
    echo "Generating JWT ECDSA P-256 key pair..."

    TMPKEY="$(mktemp)"
    trap 'rm -f "$TMPKEY"' EXIT

    # generate key in traditional EC format so ImportECPrivateKey can consume the DER bytes
    openssl ecparam -name prime256v1 -genkey -noout -out "$TMPKEY" 2>/dev/null
    openssl ec -in "$TMPKEY" -outform DER 2>/dev/null | base64 > "$SECRETS_DIR/jwt_private_key"

    echo "  Written: deploy/secrets/jwt_private_key"
fi

PRIVATE_KEY="$(cat "$SECRETS_DIR/jwt_private_key")"
dotnet user-secrets --project "$IDENTITY_DIR" set "JWT:PrivateKey" "$PRIVATE_KEY"
