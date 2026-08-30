#!/usr/bin/env bash
#
# Runs every negative case against the running app: the ways somebody could try to make the books
# say something untrue, and what the app does about each one.
#
# It builds its own parts, customer and supplier, uses them, and removes every trace afterwards —
# so it is safe against a live shop. Like check-registers.sh it mints a short-lived session rather
# than asking for a password, and deletes it on the way out.
#
# Usage:  ./scripts/check-negatives.sh
#
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$HERE/scripts/_db.sh"

TOKEN="negative-check-$(date +%s)"
TAG_FILE="$(mktemp)"

cleanup() {
  # The parts and parties this run created, and everything written against them. Scoped by the
  # names the script gives them, so nothing the shop entered is ever in range.
  db psql -q <<'SQL' > /dev/null 2>&1 || true
BEGIN;
CREATE TEMP TABLE t_products AS SELECT "Id" FROM products WHERE "ItemName" LIKE 'Check %';
CREATE TEMP TABLE t_customers AS SELECT "Id" FROM customers WHERE "Name" LIKE 'Check %';
CREATE TEMP TABLE t_suppliers AS SELECT "Id" FROM suppliers WHERE "Name" LIKE 'Check %';
CREATE TEMP TABLE t_invoices AS SELECT DISTINCT i."Id" FROM invoices i
  WHERE i."CustomerId" IN (SELECT "Id" FROM t_customers)
     OR EXISTS (SELECT 1 FROM invoice_items x WHERE x."InvoiceId" = i."Id"
                AND x."ProductId" IN (SELECT "Id" FROM t_products));
CREATE TEMP TABLE t_purchases AS SELECT DISTINCT p."Id" FROM purchases p
  WHERE p."SupplierId" IN (SELECT "Id" FROM t_suppliers)
     OR EXISTS (SELECT 1 FROM purchase_items x WHERE x."PurchaseId" = p."Id"
                AND x."ProductId" IN (SELECT "Id" FROM t_products));
CREATE TEMP TABLE t_cns AS SELECT "Id" FROM credit_notes WHERE "InvoiceId" IN (SELECT "Id" FROM t_invoices);
CREATE TEMP TABLE t_dns AS SELECT "Id" FROM debit_notes WHERE "PurchaseId" IN (SELECT "Id" FROM t_purchases);
CREATE TEMP TABLE t_payments AS SELECT DISTINCT y."Id" FROM payments y
  WHERE y."CustomerId" IN (SELECT "Id" FROM t_customers)
     OR y."SupplierId" IN (SELECT "Id" FROM t_suppliers)
     OR EXISTS (SELECT 1 FROM payment_allocations a WHERE a."PaymentId" = y."Id"
                AND (a."InvoiceId" IN (SELECT "Id" FROM t_invoices)
                  OR a."PurchaseId" IN (SELECT "Id" FROM t_purchases)
                  OR a."CreditNoteId" IN (SELECT "Id" FROM t_cns)
                  OR a."DebitNoteId" IN (SELECT "Id" FROM t_dns)));
DELETE FROM payment_allocations WHERE "PaymentId" IN (SELECT "Id" FROM t_payments);
DELETE FROM cheque_details WHERE "PaymentId" IN (SELECT "Id" FROM t_payments);
DELETE FROM party_ledger_entries WHERE "ReferenceId" IN (
  SELECT "Id" FROM t_payments UNION SELECT "Id" FROM t_invoices UNION SELECT "Id" FROM t_purchases
  UNION SELECT "Id" FROM t_cns UNION SELECT "Id" FROM t_dns);
DELETE FROM payments WHERE "Id" IN (SELECT "Id" FROM t_payments);
DELETE FROM credit_note_items WHERE "CreditNoteId" IN (SELECT "Id" FROM t_cns);
DELETE FROM credit_notes WHERE "Id" IN (SELECT "Id" FROM t_cns);
DELETE FROM debit_note_items WHERE "DebitNoteId" IN (SELECT "Id" FROM t_dns);
DELETE FROM debit_notes WHERE "Id" IN (SELECT "Id" FROM t_dns);
DELETE FROM invoice_items WHERE "InvoiceId" IN (SELECT "Id" FROM t_invoices);
DELETE FROM invoices WHERE "Id" IN (SELECT "Id" FROM t_invoices);
DELETE FROM purchase_items WHERE "PurchaseId" IN (SELECT "Id" FROM t_purchases);
DELETE FROM purchases WHERE "Id" IN (SELECT "Id" FROM t_purchases);
DELETE FROM stock_movements WHERE "ProductId" IN (SELECT "Id" FROM t_products);
DELETE FROM money_movements WHERE "Notes" = 'check' OR "Notes" LIKE 'Opening stock of Check %';
DELETE FROM products WHERE "Id" IN (SELECT "Id" FROM t_products);
DELETE FROM customers WHERE "Id" IN (SELECT "Id" FROM t_customers);
DELETE FROM suppliers WHERE "Id" IN (SELECT "Id" FROM t_suppliers);
COMMIT;
SQL
  db psql -c "DELETE FROM user_sessions WHERE \"Token\" = '$TOKEN';" > /dev/null 2>&1 || true
  rm -f "$TAG_FILE"
}
trap cleanup EXIT

# Whoever holds everything: half these cases are refused on a permission before the rule under test
# is ever reached, and a permission refusal would look like a pass.
db psql -tAc "
  INSERT INTO user_sessions (\"Id\", \"UserId\", \"Token\", \"CreatedAt\", \"ExpiresAt\", \"LastSeenAt\")
  SELECT gen_random_uuid(), u.\"Id\", '$TOKEN', now(), now() + interval '10 minutes', now()
  FROM users u JOIN roles r ON r.\"Id\" = u.\"RoleId\"
  WHERE u.\"IsActive\" AND r.\"IsSystem\"
  LIMIT 1;" > /dev/null

echo "Checking the negative cases against $(where_am_i) ..."
ANS_NEGATIVE_TOKEN="$TOKEN" python3 "$HERE/scripts/check_negatives.py"
