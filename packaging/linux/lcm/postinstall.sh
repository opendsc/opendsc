#!/bin/bash
set -euo pipefail

systemctl daemon-reload
systemctl enable opendsc-lcm
