#!/bin/bash
set -euo pipefail

if ! id "opendsc-server" &>/dev/null; then
    useradd --system --no-create-home --shell /usr/sbin/nologin opendsc-server
fi

chown root:opendsc-server /etc/opendsc/server/appsettings.json
chmod 0640 /etc/opendsc/server/appsettings.json

if command -v systemctl >/dev/null 2>&1 && [ -d /run/systemd/system ]; then
    systemctl daemon-reload
    systemctl enable opendsc-server
fi
