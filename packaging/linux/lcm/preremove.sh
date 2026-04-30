#!/bin/bash
set -euo pipefail

systemctl stop opendsc-lcm || true
systemctl disable opendsc-lcm || true
