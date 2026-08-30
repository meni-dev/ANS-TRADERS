# GST

What the app works out, what it hands the accountant, and what it deliberately leaves to them.

The shop is registered in **Tamil Nadu (33)** and files GSTR-1 and GSTR-3B monthly.

---

## How tax is decided

**Place of supply** is the customer's state code against the shop's. Different → **IGST**. Same, or
a walk-in with no address → **CGST + SGST**, split evenly. A walk-in is the shop's own state because
that is where the goods changed hands; guessing anything else would put the tax in the wrong state.

Everything is snapshotted onto the invoice line at the moment of sale — the rate, the supply type,
the HSN. A part reclassified next year does not move a return that has already been filed.

Round-off is applied to the document, not the line. The grand total lands on a whole rupee, and
`taxable + tax + roundOff = grandTotal` exactly.

## Supply types

`Domain.Enums.SupplyType` — because a rate of zero cannot say on its own what kind of supply it is,
and GSTR-1 keeps the four in different tables.

| | Meaning | Where it is reported |
|---|---|---|
| `Taxable` | Ordinary goods at a rate | B2B / B2CL / B2CS, and 3B **3.1(a)** |
| `NilRated` | Rated at nil in the tariff | Table 8, and 3B **3.1(c)** |
| `Exempt` | Exempted by notification | Table 8, and 3B **3.1(c)** |
| `NonGst` | Outside GST — petrol, diesel, alcohol | Table 8, and 3B **3.1(c)** |

Almost every spare part is `Taxable`. A taxable part must carry a rate above zero and an untaxed one
must carry zero — enforced both ways, because reporting nil-rated goods inside taxable turnover
declares turnover the shop did not have.

## The registers

Seventeen, all under **Reports & GST**, all downloadable as CSV with a UTF-8 BOM so Excel reads
Indian names correctly. Numbers are exported raw (`1234.50`), not formatted — the accountant needs a
cell they can sum.

**Books** — sales, purchase, sales returns, purchase returns, receipts and payments, expenses.

**Balances** — receivables ageing, party outstanding, stock valuation, stock movement. The first
three describe a *position*, so they take one date rather than a range and answer as at the end of
it, capped at today. That is what makes the two questions an accountant opens with answerable:
**what was on the shelf at the year end**, and **who owed what**.

Stock is valued at the rate on the item master today, not at what it cost on the date asked for —
costing here is a snapshot taken when goods are sold, not a running cost ledger. Right for a shelf
whose rates have not moved, an approximation for one whose have. See [`decisions.md`](decisions.md).

**GST** —

| Register | GSTR-1 table |
|---|---|
| B2B | invoice by invoice, one line per rate, to GSTIN holders |
| B2C Large | inter-state, unregistered, **over ₹2.5 lakh**, invoice by invoice |
| B2C Small | everything else unregistered, summarised by place of supply and rate |
| Credit / Debit Notes | CDNR — notes against registered parties |
| Nil, Exempt, Non-GST | Table 8, split inter/intra × registered/unregistered |
| HSN Summary | Table 12 — by HSN, UQC and rate together |
| Documents Issued | Table 13 — each series from-number, to-number, total, cancelled, net |
| GSTR-3B Summary | 3.1(a), 3.1(c), credit notes, 4(A)(5) ITC, 4(B)(2) reversals, and the net per head |

A supply lands in exactly **one** of B2B, B2CL, B2CS and Table 8. `check-registers.sh` asserts that
the four add back to the sales register, and that no invoice appears in two of them.

HSN Table 12 covers **every** outward supply including the untaxed ones, which is why its total is
higher than 3.1(a). Its value total equals invoice totals **less round-off**, because it sums lines
before the document is rounded.

## Goods that come back

A credit note reduces the turnover of the period it is **issued** in, not of the period the original
bill belonged to. Where it lands depends on what came back:

| Returned goods | Reduces |
|---|---|
| Taxable | GSTR-1 B2B / B2CS or CDNR, and 3B **3.1(a)** through its own "Less: credit notes" line |
| Nil rated, exempt, non-GST | GSTR-1 **Table 8**, and 3B **3.1(c)** |

`CreditNoteItem.SupplyType` is snapshotted from the line being credited, for the same reason
`InvoiceItem.SupplyType` exists: **a rate of zero cannot say what kind of supply it is.** Without it
a returned nil-rated part and a returned taxable part priced at nothing are indistinguishable, and
both used to land on the taxable line — 3.1(a) understated, 3.1(c) left standing at the full sale.
Two wrong figures that net to the same tax, which is why it survives a casual read and not a
portal cross-check.

A note against a party with no GSTIN never appears in CDNR. CDNR is registered parties only; a B2C
return reduces B2CS instead.

## What GSTR-3B does and does not do

It reports each head — CGST, SGST, IGST — separately, and stops.

The last line reads **"Output less credit, per head — before cross-head set-off"**. It is not called
*payable*, on purpose. Section 49A requires IGST credit to be used up before CGST or CGST credit,
and it may be applied against either — which way it goes is a decision, not arithmetic. Calling it
payable once told a shop holding ₹26,000 of IGST credit that it owed CGST and SGST in cash.

Cancelled documents contribute nothing but keep their row, so the series reads unbroken.

## Not built, deliberately

- **e-Invoice (IRN) and e-Way Bill** — only once turnover crosses the threshold
- **GSTR-2B reconciliation** — the one real gap. See [`state-of-play.md`](state-of-play.md)
- **Reverse charge** — hardcoded `N`; a parts counter does not buy under RCM
- **Exports, ISD, TDS/TCS, advances (11A/11B)** — none apply to this shop
- **Direct filing** — the app produces the tables; a human uploads them

## Before you file

1. `./scripts/check-registers.sh 2026-04-01 2027-03-31` — 45 checks
2. **Documents Issued** — confirm the series has no gap it cannot explain
3. **HSN Summary** — any blank HSN will have the portal reject the whole file
4. Pull the CSVs and hand them over

The app does not stop you filing something wrong. It stops the figures disagreeing with each other,
which is a different and smaller promise — and the one it can actually keep.
