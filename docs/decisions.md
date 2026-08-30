# Decisions

Things deliberately **not** built, and the reasoning. Read this before adding anything large — most
of what looks missing was considered and declined for a reason that still holds.

If you decide a reason no longer holds, that is a fine outcome. Change the entry and say why.

---

## No general ledger

**The big one.** No chart of accounts, no journal, no trial balance, no balance sheet.

A shop owner needs two things from accounting: *what did I earn*, and *something my CA can work
from*. A cost snapshot on every sale line plus an expense module answers the first in days rather
than the weeks a GL takes. The CA keeps the books in Tally and always will; this app's job is to
hand them clean registers, not to become a second set of books that disagrees with the first.

**What that costs:** opening balances, capital and drawings have no natural home. They are handled
by `MoneyMovement` — a small, explicit list of the five things a shop actually does with its own
money, which is roughly 15% of a GL and covers 95% of the cases.

**If you ever build the GL**, the first thing to do is delete `MoneyMovement`, not extend it.

## Not multi-tenant

One shop, one `shop_settings` row, one database. A second shop means real work — this is a project
for one business, not a product.

The user was explicit about this. Do not add tenancy speculatively.

## Two roles seeded, not a permission matrix per screen

22 permissions, and roles built out of them. Deliberately not one permission per screen: a matrix
nobody configures correctly protects less than a short list everybody understands.

`CostView` is the one most shops actually mean when they ask for roles — the counter must not see
the buying price or the profit.

## No OTP, no SSO

A username and a password for a shop with a handful of staff. Sessions are server-side rows, not
JWTs, so an account can be withdrawn the moment somebody leaves — a JWT would need no table and also
could not be revoked.

## Costing is a snapshot, not a method

`InvoiceItem.CostRate` is the purchase rate at the moment of sale. No FIFO, no weighted average, no
moving average.

For one shop this is honest and ten times simpler. Its limit, written down here so nobody discovers
it by surprise: **stock bought before a rate change shows the newer cost when it eventually sells.**
The purchase rate on the item master follows the newest purchase bill, so a shelf holding old and
new stock values all of it at the new rate.

## No batch, serial, warehouse or UOM conversion

One shop, one store, one unit per part. A spare parts counter does not track engine oil by batch.

## No sales orders or reservation

Goods are sold across a counter. There is no order to fulfil later.

## No manufacturing

Parts are bought and sold, not made.

## Not a POS, not offline

Browser-based, online. No barcode scanning, no thermal printer template — all five invoice templates
are A4. A shop that wants a 58mm receipt printer needs a sixth template; nothing else changes.

## No GSTR-2B reconciliation *yet*

The one gap that is a real absence rather than a decision. See
[`state-of-play.md`](state-of-play.md) — including the question to ask before building it.

---

## Smaller calls worth knowing

**Registers are not paged.** A register showing the accountant half of March is worse than none of
it. The limit this creates — a Lambda response caps at 6MB — is written down in
`deployment/README.md`. When it stops being fine, the answer is to write the export to S3 and hand
back a link, not to page the register.

**One register shape, not seventeen typed DTOs.** The server decides columns and rows; the frontend
has one table and one download. Seventeen screens and seventeen export routines would drift apart.
Numbers travel as invariant decimal text so nothing is rounded on the way out.

**Selling below cost is allowed.** Clearance happens. The Rate Drift report lists every one of them,
which is the right place for it.

**The session token lives in `localStorage`.** It is what makes the cross-origin Cloudflare-to-Lambda
split work without cookie trouble; moving to an httpOnly cookie would trade an XSS risk for a CSRF
one across two domains. A decision, not an oversight.

**`provided.al2023` with a self-contained bundle, not a managed runtime.** AWS's managed .NET
runtimes follow LTS and lag; this targets net10.0. It also means the runtime never changes under the
shop. See `deployment/README.md`.
