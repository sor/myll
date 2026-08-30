#!/usr/bin/env bash
# Build all Myll Unix utilities in frontend/apps/unix/.
# Usage: ./make_unix.sh [clean|release|debug]

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "${SCRIPT_DIR}/unix"

make "${@:-all}"
