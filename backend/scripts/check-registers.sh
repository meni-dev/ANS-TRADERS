#!/usr/bin/env bash
#
# Runs the register checks against the running app.
#
# The checks read the registers through the API, so they need a signed-in session. Rather than ask
# for a password, this mints a short-lived session row directly, runs, and removes it — it is a
# local diagnostic on the shop's own machine, and it must not become a way to hold a token around.
#
# Usage:  ./scripts/check-registers.sh [from-date] [to-date]
#         ./scripts/check-registers.sh 2026-04-01 2027-03-31
#
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$HERE/scripts/_db.sh"

TOKEN="register-check-$(date +%s)"

cleanup() {
  db psql -c "DELETE FROM user_sessions WHERE \"Token\" = '$TOKEN';" > /dev/null 2>&1 || true
}
trap cleanup EXIT

# Whoever can read every register — the checks compare registers against each other, so a role that
# sees only some of them would report failures that are really just missing permissions.
db psql -tAc "
  INSERT INTO user_sessions (\"Id\", \"UserId\", \"Token\", \"CreatedAt\", \"ExpiresAt\", \"LastSeenAt\")
  SELECT gen_random_uuid(), u.\"Id\", '$TOKEN', now(), now() + interval '10 minutes', now()
  FROM users u JOIN roles r ON r.\"Id\" = u.\"RoleId\"
  WHERE u.\"IsActive\" AND r.\"IsSystem\"
  LIMIT 1;" > /dev/null

echo "Checking the registers against $(where_am_i)${1:+, $1 to ${2:-}} ..."
ANS_REGISTER_TOKEN="$TOKEN" python3 "$HERE/scripts/check_registers.py" "$@"
