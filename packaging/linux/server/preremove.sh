#!/bin/bash
set -euo pipefail

if command -v systemctl >/dev/null 2>&1 && [ -d /run/systemd/system ]; then
    systemctl stop opendsc-server || true
    systemctl disable opendsc-server || true
fi
