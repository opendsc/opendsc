#!/bin/bash
set -euo pipefail

if ! id "opendsc-server" &>/dev/null; then
    useradd --system --no-create-home --shell /usr/sbin/nologin opendsc-server
fi

chown opendsc-server:opendsc-server /etc/opendsc/server/appsettings.json

systemctl daemon-reload
systemctl enable opendsc-server
