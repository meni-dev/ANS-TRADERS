"""
What the shop's own data says about itself.

Different question from the other two scripts. check-registers asks whether the app agrees with
itself; check-negatives asks what the app refuses. This one asks whether the figures somebody typed
in make sense — a part with no ceiling price, a GSTIN with a digit wrong, a series with a hole in
it. None of it is a defect in the app. All of it is something a shop, or its accountant, would want
to know before a return goes out.

Nothing here is fatal, so nothing here exits non-zero. It prints what it found and why it matters.
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, "..")

FINDINGS = []


def sql(query):
    out = subprocess.run(
        ["docker", "compose", "exec", "-T", "postgres", "psql", "-U", "postgres",
         "-d", "two_wheeler_spare_parts", "-tAF|", "-c", query],
        capture_output=True, text=True, cwd=ROOT).stdout
    return [line.split("|") for line in out.strip().split("\n") if line.strip()]


def finding(area, what, rows, why):
    if rows:
        FINDINGS.append((area, what, rows, why))


# ------------------------------------------------------------------ catalogue ---
finding("Catalogue", "parts with no HSN",
        sql("""SELECT "PartNumber", "ItemName" FROM products
               WHERE "Hsn" IS NULL OR "Hsn" = '' ORDER BY 1"""),
        "The portal rejects the whole HSN summary if one line has no code.")

finding("Catalogue", "HSN codes that are not 4, 6 or 8 digits",
        sql("""SELECT "PartNumber", "Hsn" FROM products
               WHERE "Hsn" !~ '^[0-9]{4}([0-9]{2}([0-9]{2})?)?$' AND "Hsn" <> '' ORDER BY 1"""),
        "Only those three lengths are accepted in a return.")

finding("Catalogue", "parts with no selling rate",
        sql("""SELECT "PartNumber", "ItemName" FROM products
               WHERE "SellingRate" = 0 AND "IsActive" ORDER BY 1"""),
        "Nothing to put on a bill — the counter has to type a price every time.")

finding("Catalogue", "parts with no MRP",
        sql("""SELECT "PartNumber", "ItemName" FROM products
               WHERE "Mrp" = 0 AND "IsActive" ORDER BY 1"""),
        "Zero means no ceiling, so the app cannot stop a sale above the printed price.")

finding("Catalogue", "parts whose selling rate is below what they cost",
        sql("""SELECT "PartNumber", "ItemName", "PurchaseRate"::text, "SellingRate"::text
               FROM products WHERE "SellingRate" > 0 AND "SellingRate" < "PurchaseRate"
               AND "IsActive" ORDER BY 1"""),
        "Every sale loses money. Clearance is a decision; this is usually a stale rate.")

finding("Catalogue", "parts whose selling rate is above their own MRP",
        sql("""SELECT "PartNumber", "SellingRate"::text, "Mrp"::text FROM products
               WHERE "Mrp" > 0 AND "SellingRate" > "Mrp" ORDER BY 1"""),
        "Billing at the shop's own rate would then be an offence under Legal Metrology.")

finding("Catalogue", "taxable parts carrying a rate of nothing",
        sql("""SELECT "PartNumber", "ItemName" FROM products
               WHERE "SupplyType" = 'Taxable' AND "GstRate" = 0 ORDER BY 1"""),
        "Reported as taxable turnover at nil tax. If they are nil rated or exempt, say so.")

finding("Catalogue", "one HSN carrying two different GST rates",
        sql("""SELECT "Hsn", string_agg(DISTINCT "GstRate"::text, ' and '),
                      string_agg("PartNumber", ', ' ORDER BY "PartNumber")
               FROM products WHERE "Hsn" <> '' GROUP BY "Hsn"
               HAVING COUNT(DISTINCT "GstRate") > 1"""),
        "Two rates under one code invite a notice. One of them is usually wrong.")

finding("Catalogue", "parts that are inactive but still hold stock",
        sql("""SELECT "PartNumber", "StockOnHand"::text FROM products
               WHERE NOT "IsActive" AND "StockOnHand" <> 0 ORDER BY 1"""),
        "The stock is in the valuation and cannot be sold.")

finding("Catalogue", "parts that sell but have no reorder level",
        sql("""SELECT p."PartNumber", COUNT(ii."Id")::text
               FROM products p JOIN invoice_items ii ON ii."ProductId" = p."Id"
               WHERE p."ReorderLevel" = 0 AND p."IsActive"
               GROUP BY p."Id", p."PartNumber" HAVING COUNT(ii."Id") >= 1 ORDER BY 1"""),
        "They will never appear in low stock, however far they fall.")

finding("Catalogue", "two parts with the same name",
        sql("""SELECT lower("ItemName"), string_agg("PartNumber", ', ' ORDER BY "PartNumber")
               FROM products GROUP BY 1 HAVING COUNT(*) > 1"""),
        "The counter picks one of them and cannot tell which.")

# ---------------------------------------------------------------- GSTIN check ---
ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"


def gstin_ok(gstin):
    if not gstin or len(gstin) != 15:
        return False
    total = 0
    for i, ch in enumerate(gstin[:14]):
        if ch not in ALPHABET:
            return False
        product = ALPHABET.index(ch) * (2 if i % 2 else 1)
        total += product // 36 + product % 36
    return ALPHABET[(36 - total % 36) % 36] == gstin[14]


parties = sql("""SELECT 'Customer', "Name", "Gstin", "StateCode" FROM customers WHERE "Gstin" <> ''
                 UNION ALL
                 SELECT 'Supplier', "Name", "Gstin", "StateCode" FROM suppliers WHERE "Gstin" <> ''""")

finding("Parties", "GSTINs whose check digit does not match",
        [[p[0], p[1], p[2]] for p in parties if not gstin_ok(p[2])],
        "One character is mistyped. The portal will reject every invoice quoting it.")

finding("Parties", "GSTINs whose state does not match the address",
        [[p[0], p[1], p[2], f"address says {p[3]}"] for p in parties
         if len(p[2]) >= 2 and p[2][:2] != p[3]],
        "The first two digits of a GSTIN are the state. One of the two is wrong, and the "
        "place of supply decides IGST against CGST+SGST.")

finding("Parties", "parties carrying a balance with no credit limit set",
        sql("""SELECT "Name", "OutstandingBalance"::text FROM customers
               WHERE "CreditLimit" = 0 AND "OutstandingBalance" > 0 ORDER BY 1"""),
        "Zero means no limit, so nothing will ever stop the next sale on credit.")

finding("Parties", "two parties with the same name",
        sql("""SELECT lower("Name"), COUNT(*)::text FROM customers GROUP BY 1 HAVING COUNT(*) > 1"""),
        "Payments and statements will be split across both.")

# ----------------------------------------------------------------- documents ---
finding("Documents", "gaps in a number series",
        sql("""WITH span AS (
                 SELECT 'Invoice' kind, "FinancialYear" fy,
                        generate_series(MIN("Sequence"), MAX("Sequence")) n
                 FROM invoices GROUP BY 2
                 UNION ALL
                 SELECT 'Purchase', "FinancialYear",
                        generate_series(MIN("Sequence"), MAX("Sequence"))
                 FROM purchases GROUP BY 2)
               SELECT s.kind, s.fy, COUNT(*)::text || ' missing',
                      'from ' || MIN(s.n)::text || ' to ' || MAX(s.n)::text
               FROM span s
               LEFT JOIN invoices i ON s.kind = 'Invoice' AND i."FinancialYear" = s.fy AND i."Sequence" = s.n
               LEFT JOIN purchases p ON s.kind = 'Purchase' AND p."FinancialYear" = s.fy AND p."Sequence" = s.n
               WHERE i."Id" IS NULL AND p."Id" IS NULL
               GROUP BY 1, 2"""),
        "A cancelled document keeps its number and stays on the register, so a gap means a row "
        "that was never written or was deleted. An auditor looks here first.")

finding("Documents", "documents in a financial year of their own",
        sql("""SELECT 'Invoice', "FinancialYear", COUNT(*)::text FROM invoices
               GROUP BY 2 HAVING COUNT(*) <= 2
               UNION ALL
               SELECT 'Purchase', "FinancialYear", COUNT(*)::text FROM purchases
               GROUP BY 2 HAVING COUNT(*) <= 2"""),
        "A year with one or two documents in it is usually a mistyped date, and it opens a "
        "return period the shop never traded in.")

finding("Documents", "large sales to somebody with no GSTIN",
        sql("""SELECT "InvoiceNumber", "CustomerName", "GrandTotal"::text FROM invoices
               WHERE "Status" <> 'Cancelled' AND ("CustomerGstin" IS NULL OR "CustomerGstin" = '')
               AND "GrandTotal" > 50000 ORDER BY "GrandTotal" DESC"""),
        "A buyer spending this much is usually a business that could have claimed the credit.")

# --------------------------------------------------------------------- money ---
finding("Money", "cheques still sitting in hand",
        sql("""SELECT c."ChequeNumber", c."Status", c."ChequeDate"::text,
                      (CURRENT_DATE - c."ChequeDate")::text || ' days old'
               FROM cheque_details c
               WHERE c."Status" NOT IN ('Cleared', 'Bounced', 'Cancelled')
               ORDER BY c."ChequeDate\""""),
        "A cheque is not money until it clears. Bank it or chase it.")

finding("Money", "money on account against no bill",
        sql("""SELECT "Name", (-"OutstandingBalance")::text FROM customers
               WHERE "OutstandingBalance" < 0 ORDER BY 1"""),
        "The shop is holding their money. One customer's advance does not settle another's debt.")

finding("Money", "day closes that came out short or over",
        sql("""SELECT "CloseDate"::text, "Difference"::text, COALESCE("Reason", '(no reason given)')
               FROM day_closes WHERE "Difference" <> 0 ORDER BY "CloseDate" DESC LIMIT 10"""),
        "Worth finding on the day rather than at the year end.")

# -------------------------------------------------------------------- report ---
print()
if not FINDINGS:
    print("Nothing to report. The catalogue, the parties, the series and the money all read cleanly.")
    sys.exit(0)

area = None
for a, what, rows, why in FINDINGS:
    if a != area:
        area = a
        print(f"\n{'─' * 74}\n{a.upper()}\n{'─' * 74}")
    print(f"\n  {what} — {len(rows)}")
    for row in rows[:8]:
        print("      " + "  ".join(str(c) for c in row if str(c).strip()))
    if len(rows) > 8:
        print(f"      … and {len(rows) - 8} more")
    print(f"      why it matters: {why}")

print(f"\n{'─' * 74}")
print(f"{len(FINDINGS)} kinds of thing worth looking at. None of them stops the app working.")
