#!/bin/sh
set -e

if [ "$(id -u)" = "0" ]; then
    mkdir -p /app/data
    # Network volumes such as Azure Files do not allow ownership changes;
    # on those, ownership comes from the mount's uid/gid options instead
    chown -R app:app /app/data 2>/dev/null || true
    exec setpriv --reuid app --regid app --init-groups "$0" "$@"
fi

if [ -z "${ASPNETCORE_Kestrel__Certificates__Default__Path:-}" ]; then
    CERT_DIR="${OPENDSC_CERT_DIR:-/app/data/certs}"
    CERT_CN="${OPENDSC_CERT_CN:-localhost}"
    mkdir -p "$CERT_DIR"

    if [ ! -f "$CERT_DIR/server.pfx" ]; then
        echo "No server certificate configured; generating self-signed certificate for CN=$CERT_CN"
        openssl req -x509 -newkey rsa:2048 -sha256 -days 825 -nodes \
            -keyout "$CERT_DIR/server.key" -out "$CERT_DIR/server.crt" \
            -subj "/CN=$CERT_CN" \
            -addext "subjectAltName=DNS:$CERT_CN,DNS:localhost,IP:127.0.0.1"
        openssl pkcs12 -export -out "$CERT_DIR/server.pfx" \
            -inkey "$CERT_DIR/server.key" -in "$CERT_DIR/server.crt" \
            -passout pass:
        rm -f "$CERT_DIR/server.key"
    fi

    export ASPNETCORE_Kestrel__Certificates__Default__Path="$CERT_DIR/server.pfx"
fi

exec dotnet OpenDsc.Server.dll "$@"
