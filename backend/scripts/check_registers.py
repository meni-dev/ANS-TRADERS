"""
Checks every register against the database it was built from.

A register is the one screen nobody double-checks — the shop hands it to the accountant and it
becomes the answer. So this asserts the things a reader would assume without being told: that a
bill's total is its cash plus its credit note plus what is still owed, that the GST tables add back
to the sales register, that a stock ledger's closing balance is what the shelf actually holds.

Two of these checks were written after the figures had already gone out wrong, which is the whole
argument for having them.

Run it with scripts/check-registers.sh, which mints the session this needs and removes it after.
"""

import json, subprocess, decimal

D = decimal.Decimal
import os
import sys

T = os.environ["ANS_REGISTER_TOKEN"]
BASE = os.environ.get("ANS_API", "http://localhost:5266")
FROM = sys.argv[1] if len(sys.argv) > 1 else "2000-04-01"
TO = sys.argv[2] if len(sys.argv) > 2 else "2099-03-31"

def reg(key):
    out = subprocess.run(["curl","-s",
        f"{BASE}/api/reports/registers/{key}?fromDate={FROM}&toDate={TO}",
        "-H", f"Authorization: Bearer {T}"], capture_output=True, text=True).stdout
    d = json.loads(out)
    d["idx"] = {c["key"]: i for i, c in enumerate(d["columns"])}
    return d

def sql(q):
    out = subprocess.run(["docker","compose","exec","-T","postgres","psql","-U","postgres",
        "-d","two_wheeler_spare_parts","-tAF,","-c",q],
        capture_output=True, text=True, cwd=os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")).stdout
    return [line.split(",") for line in out.strip().split("\n") if line.strip()]

def num(row, d, key):
    v = row[d["idx"][key]]
    return D(v) if v else D(0)

def total(d, key):
    for t in d["totals"]:
        if t["columnKey"] == key:
            return D(str(t["value"]))
    return None

ok, bad = [], []
def check(label, condition, detail=""):
    (ok if condition else bad).append(f"{label}{'' if condition else '  <-- ' + detail}")

# ---------------------------------------------------------------- per-row identities
for key, terms in [
    ("sales",            ("total", ["paid", "creditApplied", "balance"])),
    ("purchase",         ("total", ["paid", "debitApplied", "balance"])),
    ("sales-returns",    ("total", ["applied", "refunded", "onAccount"])),
    ("purchase-returns", ("total", ["applied", "refunded", "onAccount"])),
]:
    d = reg(key)
    head, parts = terms
    off = [r[d["idx"]["number"]] for r in d["rows"]
           if num(r, d, head) != sum(num(r, d, p) for p in parts)]
    check(f"{key}: every row has {head} = {' + '.join(parts)}", not off,
          f"{len(off)} rows do not: {off[:4]}")

# ---------------------------------------------------------------- against the database
d = reg("sales")
db = sql("""SELECT count(*), coalesce(sum("GrandTotal"),0), coalesce(sum("AmountPaid"),0),
                   coalesce(sum("CreditAppliedAmount"),0), coalesce(sum("BalanceDue"),0)
            FROM invoices WHERE "Status" <> 'Cancelled'
              AND "InvoiceDate" BETWEEN '%s' AND '%s'""" % (FROM, TO))[0]
check("sales: total matches the invoices table", total(d, "total") == D(db[1]), f"{total(d,'total')} vs {db[1]}")
check("sales: paid matches the invoices table", total(d, "paid") == D(db[2]), f"{total(d,'paid')} vs {db[2]}")
check("sales: credit applied matches", total(d, "creditApplied") == D(db[3]), f"{total(d,'creditApplied')} vs {db[3]}")
check("sales: balance matches", total(d, "balance") == D(db[4]), f"{total(d,'balance')} vs {db[4]}")

d = reg("sales-returns")
db = sql("""SELECT coalesce(sum("GrandTotal"),0), coalesce(sum("AppliedToInvoiceAmount"),0),
                   coalesce(sum("RefundedAmount"),0) FROM credit_notes
            WHERE "Status" <> 'Cancelled' AND "NoteDate" BETWEEN '%s' AND '%s'""" % (FROM, TO))[0]
check("sales-returns: totals match credit_notes",
      total(d, "total") == D(db[0]) and total(d, "applied") == D(db[1]) and total(d, "refunded") == D(db[2]),
      f"{total(d,'total')}/{total(d,'applied')}/{total(d,'refunded')} vs {db}")

d = reg("receipts")
db = sql("""SELECT coalesce(sum("Amount"),0) FROM payments
            WHERE "Status" = 'Posted' AND "Direction" = 'Received'
              AND "PaymentDate" BETWEEN '%s' AND '%s'""" % (FROM, TO))[0]
check("receipts: received matches the payments table", total(d, "received") == D(db[0]),
      f"{total(d,'received')} vs {db[0]}")

d = reg("expenses")
db = sql("""SELECT coalesce(sum("Amount"),0) FROM expenses
            WHERE "IsCancelled" = false AND "ExpenseDate" BETWEEN '%s' AND '%s'""" % (FROM, TO))[0]
check("expenses: total matches the expenses table", total(d, "amount") == D(db[0]),
      f"{total(d,'amount')} vs {db[0]}")

d = reg("outstanding")
db = sql("""SELECT coalesce(sum(GREATEST("OutstandingBalance",0)),0) FROM customers""")[0]
db2 = sql("""SELECT coalesce(sum(GREATEST("OutstandingBalance",0)),0) FROM suppliers""")[0]
check("outstanding: receivable matches customers", total(d, "receivable") == D(db[0]), f"{total(d,'receivable')} vs {db[0]}")
check("outstanding: payable matches suppliers", total(d, "payable") == D(db2[0]), f"{total(d,'payable')} vs {db2[0]}")

d = reg("stock-valuation")
db = sql("""SELECT coalesce(sum("StockOnHand"),0), coalesce(sum(round("StockOnHand"*"PurchaseRate",2)),0)
            FROM products WHERE "IsActive" OR "StockOnHand" <> 0""")[0]
check("stock-valuation: quantity matches products", total(d, "stock") == D(db[0]), f"{total(d,'stock')} vs {db[0]}")
check("stock-valuation: value matches products", total(d, "value") == D(db[1]), f"{total(d,'value')} vs {db[1]}")
off = [r[d["idx"]["partNumber"]] for r in d["rows"]
       if abs(num(r, d, "value") - (num(r, d, "stock") * num(r, d, "purchaseRate"))) > D("0.01")]
check("stock-valuation: every row's value is quantity x rate", not off, f"{off[:4]}")

d = reg("stock-movement")
db = sql("""SELECT coalesce(sum(CASE WHEN "Quantity" > 0 THEN "Quantity" ELSE 0 END),0),
                   coalesce(sum(CASE WHEN "Quantity" < 0 THEN -"Quantity" ELSE 0 END),0)
            FROM stock_movements""")[0]
check("stock-movement: in and out match the movements table",
      total(d, "in") == D(db[0]) and total(d, "out") == D(db[1]),
      f"{total(d,'in')}/{total(d,'out')} vs {db}")

# ---------------------------------------------------------------- GST cross-checks
sales, b2b, b2cs, hsn, cdnr = reg("sales"), reg("gstr1-b2b"), reg("gstr1-b2cs"), reg("gstr1-hsn"), reg("gstr1-cdnr")
check("GSTR-1: B2B + B2C taxable equals the sales register",
      total(b2b, "taxable") + total(b2cs, "taxable") == total(sales, "taxable"),
      f"{total(b2b,'taxable')} + {total(b2cs,'taxable')} vs {total(sales,'taxable')}")
check("GSTR-1: HSN taxable equals the sales register",
      total(hsn, "taxable") == total(sales, "taxable"),
      f"{total(hsn,'taxable')} vs {total(sales,'taxable')}")
for head in ("cgst", "sgst", "igst"):
    check(f"GSTR-1: HSN {head.upper()} equals the sales register",
          total(hsn, head) == total(sales, head), f"{total(hsn,head)} vs {total(sales,head)}")
check("GSTR-1: HSN value equals invoice totals less round-off",
      total(hsn, "value") == total(sales, "total") - total(sales, "roundOff"),
      f"{total(hsn,'value')} vs {total(sales,'total') - total(sales,'roundOff')}")

returns = reg("sales-returns")
check("GSTR-1: credit-note taxable is not more than the returns register",
      total(cdnr, "taxable") <= total(returns, "taxable"),
      f"{total(cdnr,'taxable')} vs {total(returns,'taxable')}")

b3 = reg("gstr3b")
i3 = b3["idx"]
row = {r[i3["line"]]: r for r in b3["rows"]}
out = row["Outward taxable supplies"]
check("GSTR-3B: outward line equals the sales register",
      D(out[i3["taxable"]]) == total(sales, "taxable")
      and D(out[i3["cgst"]]) == total(sales, "cgst")
      and D(out[i3["igst"]]) == total(sales, "igst"),
      f"{out[i3['taxable']]} vs {total(sales,'taxable')}")

itc = row["Input tax credit — all other ITC"]
purchase = reg("purchase")
check("GSTR-3B: input credit equals the purchase register",
      D(itc[i3["taxable"]]) == total(purchase, "taxable")
      and D(itc[i3["igst"]]) == total(purchase, "igst"),
      f"{itc[i3['taxable']]} vs {total(purchase,'taxable')}")

cn = row["Less: credit notes issued"]
check("GSTR-3B: credit-note line equals the returns register",
      -D(cn[i3["taxable"]]) == total(returns, "taxable"),
      f"{cn[i3['taxable']]} vs {total(returns,'taxable')}")

net = row["Output less credit, per head — before cross-head set-off"]
for head in ("cgst", "sgst", "igst"):
    expected = (D(out[i3[head]]) + D(cn[i3[head]]) - D(itc[i3[head]])
                - D(row["Less: ITC reversed — debit notes raised"][i3[head]]))
    check(f"GSTR-3B: net {head.upper()} is the four lines above it",
          D(net[i3[head]]) == expected, f"{net[i3[head]]} vs {expected}")

# ---------------------------------------------------------------- money that must agree
check("sales paid equals receipts received",
      total(sales, "paid") == total(reg("receipts"), "received"),
      f"{total(sales,'paid')} vs {total(reg('receipts'),'received')}")


# ---------------------------------------------------------------- deeper row-level checks
d = reg("receipts")
off = [r[d["idx"]["party"]] for r in d["rows"]
       if num(r, d, "received") > 0 and num(r, d, "paid") > 0]
check("receipts: no row is both money in and money out", not off, f"{off[:4]}")
off = [r[d["idx"]["party"]] for r in d["rows"]
       if num(r, d, "unallocated") > num(r, d, "received") + num(r, d, "paid")]
check("receipts: on-account is never more than the payment itself", not off, f"{off[:4]}")

# Every B2B line of one invoice must add back to that invoice's own taxable value.
b2b, sales = reg("gstr1-b2b"), reg("sales")
per_invoice = {}
for r in b2b["rows"]:
    per_invoice.setdefault(r[b2b["idx"]["number"]], D(0))
    per_invoice[r[b2b["idx"]["number"]]] += num(r, b2b, "taxable")
sales_taxable = {r[sales["idx"]["number"]]: num(r, sales, "taxable") for r in sales["rows"]}
off = [n for n, v in per_invoice.items() if sales_taxable.get(n) != v]
check("GSTR-1 B2B: each invoice's rate lines add back to its taxable value", not off, f"{off[:4]}")

# Invoice value must be one figure per invoice, not a different one per rate line.
seen = {}
off = []
for r in b2b["rows"]:
    n, v = r[b2b["idx"]["number"]], num(r, b2b, "invoiceValue")
    if seen.setdefault(n, v) != v:
        off.append(n)
check("GSTR-1 B2B: invoice value is the same on every line of an invoice", not off, f"{off[:4]}")

# Stock movements are a running balance; each row's balance is the one before it plus the move.
d = reg("stock-movement")
running, off = {}, []
for r in d["rows"]:
    part = r[d["idx"]["partNumber"]]
    moved = num(r, d, "in") - num(r, d, "out")
    expected = running.get(part, D(0)) + moved
    if num(r, d, "balance") != expected:
        off.append(f"{part}@{r[d['idx']['when']]}")
    running[part] = num(r, d, "balance")
check("stock-movement: every balance follows from the one before it", not off,
      f"{len(off)} breaks: {off[:3]}")

# The closing balance of the ledger has to be what the shelf says it holds.
val = reg("stock-valuation")
on_hand = {r[val["idx"]["partNumber"]]: num(r, val, "stock") for r in val["rows"]}
off = [p for p, b in running.items() if p in on_hand and on_hand[p] != b]
check("stock-movement: closing balance matches stock valuation", not off, f"{off[:4]}")

# Nothing dated outside the range asked for.
for key, col in [("sales", "date"), ("purchase", "date"), ("sales-returns", "date"),
                 ("receipts", "date"), ("expenses", "date")]:
    d = reg(key)
    off = [r[d["idx"][col]] for r in d["rows"] if not (FROM <= r[d["idx"][col]] <= TO)]
    check(f"{key}: every row falls inside the range asked for", not off, f"{off[:3]}")

for line in ok:
    print("  ok    ", line)

print()

if bad:
    print(f"{len(bad)} CHECK(S) FAILED - the registers do not agree with the books:")
    for line in bad:
        print("  FAIL  ", line)
    raise SystemExit(1)

print(f"All {len(ok)} checks passed. Every register agrees with the data behind it.")
