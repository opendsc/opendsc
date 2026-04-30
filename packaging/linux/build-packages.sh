#!/bin/bash
# Builds .deb and .rpm packages for OpenDSC components using FPM.
# Requires: fpm (gem install fpm), rpmbuild (rpm-build package)
#
# Usage:
#   ./build-packages.sh --version 0.5.1 --artifacts-dir /path/to/artifacts --output-dir /path/to/output --arch amd64

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

VERSION=""
ARTIFACTS_DIR=""
OUTPUT_DIR=""
ARCH="amd64"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --version)    VERSION="$2";       shift 2 ;;
        --artifacts-dir) ARTIFACTS_DIR="$2"; shift 2 ;;
        --output-dir) OUTPUT_DIR="$2";    shift 2 ;;
        --arch)       ARCH="$2";          shift 2 ;;
        *) echo "Unknown argument: $1" >&2; exit 1 ;;
    esac
done

if [[ -z "$VERSION" || -z "$ARTIFACTS_DIR" || -z "$OUTPUT_DIR" ]]; then
    echo "Usage: $0 --version <ver> --artifacts-dir <dir> --output-dir <dir> [--arch amd64|arm64]" >&2
    exit 1
fi

mkdir -p "$OUTPUT_DIR"

STAGING_DIRS=()

cleanup_staging_dirs() {
    for dir in "${STAGING_DIRS[@]}"; do
        rm -rf "$dir"
    done
}

trap cleanup_staging_dirs EXIT

build_package() {
    local name="$1"
    local staging_dir="$2"
    local description="$3"
    local depends_args=("${@:4}")

    for format in deb rpm; do
        echo "Building $name.$format ($ARCH)..."
        fpm \
            --input-type dir \
            --output-type "$format" \
            --name "$name" \
            --version "$VERSION" \
            --architecture "$ARCH" \
            --description "$description" \
            --url "https://github.com/opendsc/opendsc" \
            --license MIT \
            --maintainer "OpenDSC <opendsc@users.noreply.github.com>" \
            --vendor "OpenDSC" \
            "${depends_args[@]}" \
            --package "$OUTPUT_DIR" \
            --chdir "$staging_dir" \
            .
    done
}

# ── opendsc-resources ────────────────────────────────────────────────────────

RESOURCES_STAGING="$(mktemp -d)"
STAGING_DIRS+=("$RESOURCES_STAGING")

install -Dm755 "$ARTIFACTS_DIR/publish/OpenDsc.Resources" \
    "$RESOURCES_STAGING/usr/bin/OpenDsc.Resources"

install -Dm644 "$ARTIFACTS_DIR/publish/OpenDsc.Resources.dsc.manifests.json" \
    "$RESOURCES_STAGING/usr/bin/OpenDsc.Resources.dsc.manifests.json"

build_package "opendsc-resources" "$RESOURCES_STAGING" \
    "OpenDSC DSC resources for Windows, SQL Server, FileSystem, XML, JSON, and Archive" \
    --depends "dotnet-runtime-10.0"

# ── opendsc-lcm ──────────────────────────────────────────────────────────────

LCM_STAGING="$(mktemp -d)"
STAGING_DIRS+=("$LCM_STAGING")

LCM_BINARY_SOURCE="$ARTIFACTS_DIR/Lcm/opendsc-lcm"
if [[ ! -f "$LCM_BINARY_SOURCE" ]]; then
    LCM_BINARY_SOURCE="$ARTIFACTS_DIR/Lcm/OpenDsc.Lcm"
fi

if [[ ! -f "$LCM_BINARY_SOURCE" ]]; then
    echo "LCM executable not found. Expected one of:" >&2
    echo "  $ARTIFACTS_DIR/Lcm/opendsc-lcm" >&2
    echo "  $ARTIFACTS_DIR/Lcm/OpenDsc.Lcm" >&2
    exit 1
fi

install -Dm755 "$LCM_BINARY_SOURCE" \
    "$LCM_STAGING/usr/bin/opendsc-lcm"

install -Dm644 "$REPO_ROOT/packaging/systemd/opendsc-lcm.service" \
    "$LCM_STAGING/lib/systemd/system/opendsc-lcm.service"

install -Dm644 "$ARTIFACTS_DIR/Lcm/appsettings.json" \
    "$LCM_STAGING/etc/opendsc/lcm/appsettings.json"

build_package "opendsc-lcm" "$LCM_STAGING" \
    "OpenDSC Local Configuration Manager — monitors and remediates DSC configurations" \
    --config-files "/etc/opendsc/lcm/appsettings.json" \
    --after-install "$SCRIPT_DIR/lcm/postinstall.sh" \
    --before-remove "$SCRIPT_DIR/lcm/preremove.sh"

# ── opendsc-server ───────────────────────────────────────────────────────────

SERVER_STAGING="$(mktemp -d)"
STAGING_DIRS+=("$SERVER_STAGING")

# Include all framework-dependent runtime files and keep apphost colocated with
# its runtime artifacts so framework-dependent execution works reliably.
mkdir -p "$SERVER_STAGING/usr/lib/opendsc/server"
cp -r "$ARTIFACTS_DIR/Server/." "$SERVER_STAGING/usr/lib/opendsc/server/"
install -d "$SERVER_STAGING/usr/bin"
ln -s ../lib/opendsc/server/opendsc-server \
    "$SERVER_STAGING/usr/bin/opendsc-server"

install -Dm644 "$REPO_ROOT/packaging/systemd/opendsc-server.service" \
    "$SERVER_STAGING/lib/systemd/system/opendsc-server.service"

install -Dm640 "$ARTIFACTS_DIR/Server/appsettings.json" \
    "$SERVER_STAGING/etc/opendsc/server/appsettings.json"

build_package "opendsc-server" "$SERVER_STAGING" \
    "OpenDSC Pull Server — REST API and Blazor admin UI for centralized DSC configuration" \
    --depends "dotnet-runtime-10.0" \
    --config-files "/etc/opendsc/server/appsettings.json" \
    --after-install "$SCRIPT_DIR/server/postinstall.sh" \
    --before-remove "$SCRIPT_DIR/server/preremove.sh"

echo "Packages written to $OUTPUT_DIR"
