#!/bin/bash
set -euo pipefail

if command -v systemctl >/dev/null 2>&1 && [ -d /run/systemd/system ]; then
    systemctl stop opendsc-lcm || true
    systemctl disable opendsc-lcm || true
fi
