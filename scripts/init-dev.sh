#!/usr/bin/env bash
# Initialises secrets required for local development and Docker Compose.
# Safe to re-run: existing secrets are never overwritten.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SECRETS_DIR="$ROOT_DIR/deploy/secrets"
IDENTITY_DIR="$ROOT_DIR/ReLiveWP.Backend.Identity"

mkdir -p "$SECRETS_DIR"

# -- JWT ECDSA P-256 key pair --------------------------------------------------
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

# Derive the public key (SPKI DER, base64) so verifiers can validate tokens without the private
# key. Not secret; distributed to every verifying service as JWT:PublicKey (docker secret / config).
if [ ! -f "$SECRETS_DIR/jwt_public_key" ]; then
    base64 -d "$SECRETS_DIR/jwt_private_key" \
        | openssl ec -inform DER -pubout -outform DER 2>/dev/null \
        | base64 > "$SECRETS_DIR/jwt_public_key"
    echo "  Written: deploy/secrets/jwt_public_key"
fi

# -- Connection secret key -----------------------------------------------------
# AES-256 key wrapping stored credentials for services linked with a username and password
# rather than OAuth. ConnectedServices refuses to start without it once such a service exists.
if [ ! -f "$SECRETS_DIR/connection_secret_key" ]; then
    openssl rand -base64 32 | tr -d '\n' > "$SECRETS_DIR/connection_secret_key"
    echo "  Written: deploy/secrets/connection_secret_key"
fi

PRIVATE_KEY="$(cat "$SECRETS_DIR/jwt_private_key")"
dotnet user-secrets --project "$IDENTITY_DIR" set "JWT:PrivateKey" "$PRIVATE_KEY"
