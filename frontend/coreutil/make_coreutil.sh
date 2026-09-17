#!/usr/bin/env bash
# Build all Myll core utilities in frontend/coreutil/.
# Usage: ./make_coreutil.sh [clean|release|debug]

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "${SCRIPT_DIR}"

make "${@:-all}"
