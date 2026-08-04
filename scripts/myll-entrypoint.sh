#!/bin/sh
set -e

if [ "$1" = "--test" ]; then
	shift
	TEST_DLL=$(ls /src/testing/bin/Release/*/myll_test.dll 2>/dev/null | head -n 1)
	[ -z "$TEST_DLL" ] && { echo "Myll test DLL not found"; exit 1; }
	exec dotnet test "$TEST_DLL" "$@"
fi

MYLL_BIN=$(ls /src/frontend/bin/Release/*/myll 2>/dev/null | head -n 1)
[ -z "$MYLL_BIN" ] && { echo "Myll frontend executable not found"; exit 1; }
exec "$MYLL_BIN" "$@"
