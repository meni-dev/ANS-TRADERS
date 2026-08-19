#!/usr/bin/env bash
#
# Restores a dump into a throwaway database and checks that the shop's books still add up inside it.
#
# A backup nobody has restored is not a backup, it is a file. This is the script that turns one into
# the other — run it after the first backup, and again whenever the schema changes.
#
# The restore always happens in the local compose database, even for a dump taken from Neon or
# Supabase. That is the stronger test: it proves the dump can be brought back somewhere other than
# where it came from, which is the situation you would actually be in.
#
# Usage:  ./scripts/verify-backup.sh /path/to/ans-traders-2026-08-19_2100.dump
#
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DUMP="${1:?Pass the dump file to verify}"
SCRATCH="restore_check_$(date +%s)"

compose() { docker compose --project-directory "$HERE" "$@"; }
psql_scratch() { compose exec -T postgres psql --username=postgres --dbname="$SCRATCH" -tA "$@"; }

cleanup() {
  compose exec -T postgres psql --username=postgres --dbname=postgres \
    -c "DROP DATABASE IF EXISTS \"$SCRATCH\";" > /dev/null 2>&1 || true
}
trap cleanup EXIT

echo "Restoring $(basename "$DUMP") into $SCRATCH ..."
compose exec -T postgres psql --username=postgres --dbname=postgres \
  -c "CREATE DATABASE \"$SCRATCH\";" > /dev/null

# --exit-on-error, so a dump that restores 90% of itself counts as a failure. A partial restore is
# the worst outcome available: it looks like a working database and is missing rows.
compose exec -T postgres pg_restore --username=postgres --dbname="$SCRATCH" \
  --no-owner --no-acl --exit-on-error < "$DUMP"

fail=0
check() {
  local label="$1" sql="$2"
  local result
  result="$(psql_scratch -c "$sql")"

  if [ "$result" = "0" ]; then
    printf '  %-46s ok\n' "$label"
  else
    printf '  %-46s FAILED (%s)\n' "$label" "$result"
    fail=1
  fi
}

echo "Checking the restored copy:"

check "customer balance matches its ledger" "
  SELECT count(*) FROM customers c
  WHERE round(c.\"OutstandingBalance\", 2) <> round(
    coalesce((SELECT sum(e.\"Amount\") FROM party_ledger_entries e WHERE e.\"CustomerId\" = c.\"Id\"), 0), 2);"

check "supplier balance matches its ledger" "
  SELECT count(*) FROM suppliers s
  WHERE round(s.\"OutstandingBalance\", 2) <> round(
    coalesce((SELECT sum(e.\"Amount\") FROM party_ledger_entries e WHERE e.\"SupplierId\" = s.\"Id\"), 0), 2);"

# Opening stock is written as a movement of its own, so it is already inside this sum. Adding the
# OpeningStock column on top would count it twice — the same invariant the app checks on its
# dashboard, deliberately worded the same way.
check "stock on hand matches its movements" "
  SELECT count(*) FROM products p
  WHERE round(p.\"StockOnHand\", 3) <> round(
    coalesce((SELECT sum(m.\"Quantity\") FROM stock_movements m WHERE m.\"ProductId\" = p.\"Id\"), 0), 3);"

check "live invoices balance three ways" "
  SELECT count(*) FROM invoices
  WHERE \"Status\" <> 'Cancelled'
    AND round(\"GrandTotal\" - \"AmountPaid\" - \"CreditAppliedAmount\", 2) <> round(\"BalanceDue\", 2);"

check "payments allocate to no more than themselves" "
  SELECT count(*) FROM payments
  WHERE round(\"AllocatedAmount\" + \"UnallocatedAmount\", 2) <> round(\"Amount\", 2);"

# Not a books check — a schema one. A dump that restores its data but not its migration history
# would look healthy and then be refused by the app on the next deploy.
check "migration history came across" "
  SELECT CASE WHEN count(*) > 0 THEN 0 ELSE 1 END FROM \"__EFMigrationsHistory\";"

# Somebody has to be able to get in. A dump with no accounts restores cleanly and locks you out.
check "at least one account can still sign in" "
  SELECT CASE WHEN count(*) > 0 THEN 0 ELSE 1 END FROM users u
  JOIN roles r ON r.\"Id\" = u.\"RoleId\"
  WHERE u.\"IsActive\" AND r.\"IsSystem\";"

rows="$(psql_scratch -c 'SELECT count(*) FROM invoices;')"
echo "  invoices restored: $rows"

if [ "$fail" -ne 0 ]; then
  echo "RESTORE VERIFIED FAILED — this dump is not something to rely on." >&2
  exit 1
fi

echo "RESTORE VERIFIED — the dump restores cleanly and the books add up inside it."
