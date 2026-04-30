#!/bin/bash
set -euo pipefail

systemctl stop opendsc-server || true
systemctl disable opendsc-server || true
