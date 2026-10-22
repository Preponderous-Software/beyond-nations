#!/bin/bash
# Beyond Nations' playtest harness is playtester's `pt` command with the
# beyond-nations profile (docs/PLAYTESTING.md). This shim runs it against
# this checkout, so the commands of the first kit keep working:
#
#   PT_SESSION=s01 tools/playtest/pt.sh start --seed 42
#   PT_SESSION=s01 tools/playtest/pt.sh state
#   PT_SESSION=s01 tools/playtest/pt.sh stop
#
# The game's half of the kit, --playtest-out, lives in
# src/BeyondNations.Desktop/playtest/. playtester is pinned in
# tools/playtest/playtester.ref.

set -u
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$HERE/../.." && pwd)"

export PT_PROFILE="${PT_PROFILE:-beyond-nations}"
export PT_GAME_DIR="${PT_GAME_DIR:-$REPO}"
export PT_ROOT="${PT_ROOT:-/tmp/bn-playtest}"

if command -v pt >/dev/null 2>&1 && pt --version >/dev/null 2>&1; then
  exec pt "$@"
fi
if python3 -c 'import playtester.harness' >/dev/null 2>&1; then
  exec python3 -m playtester "$@"
fi
echo "pt.sh: playtester is not installed. Install the pinned version:" >&2
echo "  pip install 'git+https://github.com/dmccoystephenson/playtester@$(cat "$HERE/playtester.ref")'" >&2
echo "(docs/PLAYTESTING.md)" >&2
exit 1
