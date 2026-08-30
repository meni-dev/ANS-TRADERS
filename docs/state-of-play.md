# State of play

As at **20 August 2026**. Update this page when something moves.

---

## Health

| | |
|---|---|
| Backend build | 0 errors |
| Unit tests | 155 passing |
| Register checks | 48/48 passing |
| Negative-case checks | 69/69 passing |
| Frontend | `tsc` clean, lint clean, build clean |
| Migrations | 29, all applied |

## What exists

Billing with five printable A4 templates · purchases · sales and purchase returns · receipts,
payments, cheques and party statements · stock with a full ledger and coded adjustment reasons ·
expenses and profit and loss · cash, day close, opening float, bank transfers, capital and drawings ·
catalogue with bulk import · 18 registers including the GSTR-1 tables and a 3B summary · roles and
22 permissions · audit trail · books lock · dead stock, rate drift and a velocity-driven buying list ·
backup with a restore that is actually verified.

## Open

### The one real gap

**GSTR-2B reconciliation.** Input credit can only be claimed on invoices the supplier actually
filed. Today the 3B input figure is simply everything the shop entered — if a supplier never filed,
the shop claims credit it is not entitled to and learns about it in a notice.

Everything a match needs is already stored on the purchase: `SupplierGstin`,
`SupplierInvoiceNumber`, `InvoiceDate`, and the taxable and tax amounts.

**Ask before building it:** does the accountant already do this in Tally? If they do, a second
implementation will disagree with theirs on some month and both will stop being trusted. If nobody
does it, it is the most valuable thing left.

Two sizes if it goes ahead. The **80% version** — import the portal's 2B JSON and show three lists
*(matched · in books not in 2B · in 2B not in books)* — is most of the value. The full version adds
a manual link screen, accept/pending states, and feeds only matched ITC into 3B. The hard part in
both is matching invoice numbers: suppliers file `SMP/1188` where the shop typed `SMP-1188` or
`1188`, so no rule reaches 100% and a manual link screen is not optional.

No GSP API is needed. 2B is generated once a month on the 14th and does not change — downloading
the JSON is a minute of work.

### Not yet deployed

The app runs on `localhost` only. `backend/deployment/` has the full guide for Lambda + Cloudflare +
Neon, the Worker, and `aws-lambda-tools-defaults.json`, but nothing has been deployed. **Until it
is, none of this is usable from the shop counter.**

One thing to check before deploying: whether a managed `dotnet10` runtime exists in your region
(`aws lambda list-layers --compatible-runtime dotnet10`). The current setup ships self-contained on
`provided.al2023` and works either way.

### Smaller, if they ever matter

- Sales below cost are allowed and only surfaced in the Rate Drift report — no warning at the counter
- No thermal (58mm/80mm) invoice template; all five are A4
- No barcode scanning
- Costing is a snapshot, not FIFO or weighted average — see [`decisions.md`](decisions.md)

## Known data issues in the current database

The database currently holds **test data from a fresh-shop walkthrough**, not real trade.

- Three parts (`BLB-400`, `CLU-200`, `NIL-600`) once read negative through August because their
  opening stock was dated the day it was keyed rather than the day the books began. Both the rule
  and the history are fixed; `check-registers.sh` reports any recurrence under *worth knowing*.
- **The invoice and purchase series have large gaps** — 167 invoice numbers between 12 and 183, and
  26 purchase numbers. They were consumed by probe documents that were created through the API and
  then deleted straight from the database during testing. A shop can never cause this: documents are
  cancelled, never deleted, and a cancelled document keeps its number. **If this database is ever to
  become the shop's real books, start from an empty one** — a series with 167 phantom numbers cannot
  be explained to an auditor.
- **Invoice numbers 0060–0062 do not exist.** Consumed by the first version of the document
  numbering fix, before the number claim was made transactional. The dashboard's numbering check
  reports them, correctly. Nothing else in any series has a gap.
- A purchase dated **2019-04-01** exists from testing the date floor, creating a stray FY 2019-20.
  `BooksStartFrom` is now set to 2026-04-01 so it cannot happen again, but that row is still there.
- Several `QA-*` products and customers remain from testing.

`./scripts/check-data.sh` lists everything else worth knowing about the data — parts with no MRP,
one HSN carrying three GST rates, a GSTIN whose state disagrees with its address, a day close that
came out ₹10,989 over. All of it is data to correct, none of it is a defect.

Backups sit in `~/ANS-Traders-Backups/`. The one taken immediately before the walkthrough is
`ans-traders-2026-08-20_0621.dump`.

## History worth knowing

Two rounds of review produced most of the current shape.

**A QA pass** found twelve defects, all fixed. The three that mattered: document numbering was not
concurrency-safe and failed with a raw 500 when two people billed at once; counter staff could read
salaries, the drawer and every party's dues; and sign-in had no rate limit at all.

**A negative-case sweep** across stock, money and GST found four holes, all closed. Three were the
same shape — *nothing checked that a reversal could still be reversed*: cancelling a purchase or a
credit note took stock off a shelf that no longer had it, and a drawing emptied a till past zero.
The fourth was a counted day accepting cash afterwards. GST held up under every case, and billing
the last unit from six counters at once produced exactly one bill.

**A fresh-shop walkthrough** — everything wiped, a new shop opened and traded for a month — found
fourteen gaps, thirteen now closed. They were not bugs; they were things a real shop needs that the
app had no way to record. The pattern worth remembering: *the app was correct about everything it
knew, and silent about several things it did not.*

One finding in that pass was **wrong and was retracted** after retesting — the books lock can freeze
a month that has ended. Retracting it was worth more than the count.

## The other documents

Two published pages sit alongside these, for conversations rather than for code:

- **Product overview** — what the system does, in the words a shop owner would use.
  https://claude.ai/code/artifact/a4a47d42-8fa0-44a1-a0ef-c0368e64e795
- **Defect report** — the twelve defects the QA pass found, each with its evidence and its fix.
  https://claude.ai/code/artifact/ffba586a-a13c-4003-9c8b-c4dea16aa753
- **A shop's first month** — the fresh-shop walkthrough and the fourteen gaps it found.
  https://claude.ai/code/artifact/0260df61-ffba-4349-abdb-cef126e5b5f0
