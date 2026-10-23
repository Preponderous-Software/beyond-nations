#!/bin/bash
# The scripted session that proves the playtest kit end to end on this
# machine: title -> Start -> Begin -> chop trees until the "Gather wood"
# objective completes, confirmed in events.jsonl. It is the beyond-nations
# profile's smoke script, run by playtester (`pt smoke`).
#
#   PT_SESSION=smoke tools/playtest/smoke.sh
#
# Prints PASS and exits 0 when the objective completed; always stops its
# session.

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec "$HERE/pt.sh" smoke --session "${PT_SESSION:-smoke}"
