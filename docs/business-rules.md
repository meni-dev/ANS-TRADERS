# Business rules

Every rule the app enforces, and why it exists. **Read the relevant section before changing
anything near it** — most of these look arbitrary until you know the reason, and several were added
after the wrong thing had already happened.

Rules live in the service, next to the data they read. Shape-only checks live in validators.

---

## Selling

| Rule | Why |
|---|---|
| Cannot bill more than is on the shelf | Stock is what the shop physically has. A negative shelf is not a state that exists |
| Cannot bill **above MRP** | Selling above the printed MRP is an offence under the Legal Metrology rules — so it blocks, not warns. `Mrp = 0` means nobody entered one, not that the part may be given away |
| Cannot discount a line **100%** | The goods left the shelf and the bill says nothing was sold. Free issue belongs in a stock adjustment with reason `FreeIssue` |
| A bill cannot come to **zero** | However it got there — every line free, or a bill discount that swallowed the lot |
| Same product cannot appear twice on one bill | Two lines for one part is a data-entry slip, and it makes the returnable quantity ambiguous |
| Cannot be dated in the future, or before `BooksStartFrom` | Only the future used to be bounded, so one mistyped year opened a financial year that had closed |
| A **credit limit** is checked against what the bill would leave owing | After discount and tax, because those decide whether it is really crossed. `CreditLimit = 0` means no limit |
| A non-credit bill's `amountPaid` must be the bill | Cash and card sales settle in full. A rupee of tolerance, because the screen and the server each compute the total and can differ by a paisa on round-off |
| A **bill-level discount** needs its own permission, and is written to the audit trail | It is the commonest way a counter leaks money |

Tax is decided by **place of supply**: the customer's state code against the shop's. A walk-in with
no address is the shop's own state — that is where the goods changed hands.

## Returns

| Rule | Why |
|---|---|
| Cannot return more than was billed, less what has already come back | `CK_invoice_items_returned_quantity` enforces it in the database too, for two returns landing at once |
| Cannot be dated in the future or before its bill | Goods cannot come back before they were sold |
| Needs a reason | Required on the printed note under Rule 53(1A)(g), and the first thing an auditor asks |
| Cannot refund more cash than the note is worth | |
| **A bill with a live credit note cannot be cancelled** | The goods would come back twice, and the customer holds a note against a bill that would no longer exist |

A credit note settles **three ways**: applied to the bill, refunded in cash, or left as credit on
the customer's account. All three appear on the register — the third was missing once and the
register did not add up.

## Buying

| Rule | Why |
|---|---|
| Supplier bill number is required, and cannot repeat for the same supplier | Entering one bill twice doubles the stock and the payable |
| Cannot be dated in the future or before `BooksStartFrom` | |
| A purchase **moves the item master's purchase rate** to the net rate on that bill | Margin, valuation, dead-stock value and the cost stamped on the next sale all read that one number. The newest bill wins — a shop asked what a part costs answers with what it paid last time, not a weighted mean |

## Money

| Rule | Why |
|---|---|
| Cannot allocate more than the payment, or more than the document still owes | |
| One party's money cannot settle another party's document | |
| A cheque needs a number, and cannot be dated more than six months ahead | A cheque is stale after three months; anything beyond will never be banked |
| A **pending** cheque moves no balance | Cash is a figure somebody physically counts. It cannot include a promise |
| Cancelling or bouncing needs `PaymentCancel` | Both undo a receipt that already moved a balance |
| Adjusting a party balance by hand needs `PaymentCancel` | It changes what somebody owes with no document behind it — the strongest thing on that screen |

### Cash

The drawer moves on: cash receipts, cash payments out, cash expenses, and **money movements** —
the opening float, bank-to-till and back, capital introduced, drawings.

| Rule | Why |
|---|---|
| **A day cannot be closed with a negative till** | Physical cash has a floor at zero. Reaching there means money went out that never came in — almost always a bank withdrawal nobody recorded. Refused at the close, not at each payment, because a day is entered in whatever order the counter gets to it |
| The **opening float** can be recorded once | A second one would be counted again in every close from then on |
| Bank↔till always affects cash; opening stock never does | By definition, in both directions |
| A close is a **reconciliation point** — the cash book snaps to `CountedCash` | Without it the book and the drawer drift apart by every difference ever explained, and two screens give two answers to "how much cash is there" |
| The next day opens on the previous day's **counted** cash, not its expected | Whatever the book said, the notes physically there are what the next day starts with |
| A difference either way needs a reason | An unexplained surplus signals a mis-keyed bill as loudly as a shortage signals a missing note |

## Stock

| Rule | Why |
|---|---|
| An adjustment needs a **coded reason** | Damage, Expiry, TheftOrMissing, CountingError, FreeIssue, Scrapped, Other. Free text cannot answer "how much did I lose to damage this year" |
| Cannot count to a negative | |
| A recount that matches is refused | A zero-quantity movement clutters the ledger with rows that moved nothing |
| The loss report **excludes positive adjustments** | Found stock is not a loss, and mixing the two makes the total meaningless |

Opening stock is written as a stock movement **and** as a `MoneyMovement` of kind `OpeningStock`, so
"what did I put into this business" has an answer.

## The catalogue

| Rule | Why |
|---|---|
| Part number is unique | |
| GST rate must be a **real slab** — 0, 0.25, 3, 5, 12, 18, 28 | Anything else reaches GSTR-1 before anyone notices |
| A **taxable** part must carry a rate above zero; nil-rated, exempt and non-GST parts must carry zero | See [`gst.md`](gst.md) |
| HSN must be **4, 6 or 8 digits** | The portal rejects a whole return over one malformed HSN |
| Selling rate cannot exceed MRP | Billing refuses it anyway, so the price would be unusable the moment it was set |
| GSTIN must pass its **check digit**, and its first two characters must match the state | A mistyped GSTIN reaches GSTR-1 and the buyer cannot claim credit. A GSTIN from a different state bills tax for one state under a registration belonging to another |

Import is **all or nothing**: one bad row among five thousand writes nothing. Preview and import run
the same examination, so a preview can never promise what the import refuses. Re-importing a price
list deliberately does **not** touch `OpeningStock` — that would rewrite the shelf.

## Access

Two starting roles, seeded by migration: **Owner** (everything, built-in, cannot be edited) and
**Counter Staff** (raise a bill, take goods back, see stock, take payments).

| Rule | Why |
|---|---|
| Sign-in locks after **5** wrong passwords, for a window growing 1, 4, 9 minutes, capped at 30 | Held on the user row, not in memory — several copies of the API run at once and an in-process counter would just be spread across them |
| The lock is checked **before** the password is hashed | A locked account then costs an attacker nothing to find and gains them nothing |
| Wrong username and wrong password give one identical message | Otherwise accounts can be enumerated by trying names |
| Resetting a password clears the lock and drops that person's sessions | |
| The **last person who can manage people** cannot be deactivated or demoted | Otherwise nobody can add anyone back |
| Nobody can deactivate themselves | |

## A document is dated by the shop's clock, and never in the future

`DocumentDate()` refuses anything after `IShopClock.Today`. It used to allow `UtcNow + 1 day`, which
on a UTC server made **tomorrow** a legal bill date — while an expense or a day close, which already
asked the shop clock, refused it. One rule now, from one clock.

Back-dating stays allowed: bills genuinely get keyed days late.

## Stock moves on the document's date, not the day it was typed

`StockMovement.MovementDate` is the day the goods moved; `MovedAt` is when the row was written. They
answer different questions and a shop needs both. Before the column existed, a bill dated the 5th
and keyed on the 20th put its movement on the 20th — so the stock register and the sales register
disagreed about the week the shelf emptied, by as much as fifteen days in this shop's own data.

Every register that asks *what happened in this period* reads `MovementDate`.

Two consequences to know:

- The stock register **computes** its running balance down the page rather than reading
  `BalanceAfter`, which was worked out when the row was written. Same trap as the party statement,
  same answer — see the ledger note in [`architecture.md`](architecture.md)
- A filtered register opens from `GetStockBalanceBeforeAsync`, so page one starts from the truth
  rather than from zero

## A back-dated bill is checked against the shelf as it stood

`EnsureAvailableOnAsync` compares against the balance **on the document's own date**, not against
today's `StockOnHand`. Back-date a bill to a week the shelf was empty and today's stock would
otherwise wave it through, leaving the books showing goods sold before they arrived — replaying this
shop's own movements in document-date order found six such lines.

The refusal names both explanations, because it is genuinely one or the other and only the counter
can say which:

> On 05 Aug 2026 the shelf held 0 PCS of 'Clutch Plate', so 5 cannot be billed on that date. Either
> the date is wrong, or the purchase that brought them in has not been entered yet.

A document dated today takes the old single-field check — the common case does not pay for a query.

## Opening stock is dated when the books begin

Not the day somebody typed the part in. `ProductService` and `ProductImportService` both read
`ShopSettings.BooksStartFrom` and stamp the opening movement — and the money line that records what
the shelf was worth — with it.

The consequence of the old rule took months to surface and then surfaced everywhere at once. A shop
that keys its catalogue in August and then enters April's bills has every sale sitting *before* the
stock it sold: three parts here read −2, −12 and −5 through the middle of August, and a valuation at
any date in that window was meaningless. A part is not on the shelf because somebody typed it; it
was on the shelf when the books opened, which is what "opening" means.

A shop that has not said when its books begin has nothing better than today. A start date in the
future is ignored — that is a typo, and dating stock forward would put it beyond every register that
could show it.

**If goods genuinely arrived in August, the entry is a purchase, not opening stock.**

## The credit limit is measured against what was owed coming in

`InvoiceService` reads the customer's balance **before** the party ledger records the bill, because
the ledger is about to add it to the running total. Reading it afterwards counted the bill twice and
refused every customer at roughly half the limit the shop had set — with a message quoting a figure
nobody could reconcile.

Zero is an unset field, not a limit of nothing. Money taken at the counter never reaches the account
and so never counts against the limit.

## An inactive part is closed in both directions

Billing one is refused and so is buying one. Blocking only the sale let stock climb on a part nobody
was allowed to move, quietly, with every screen offering no explanation.

## A refund cannot be larger than the credit the party holds

Money paid **to** a customer, or received **from** a supplier, is a refund — the only reason it
moves that way. It is checked against the credit standing on that party's account, so the same
hundred rupees cannot be handed back twice and quietly flip the account into the customer owing the
shop money they never borrowed.

Refunding *on* the credit note itself is a different path with its own limit; this one covers the
credit still standing afterwards.

## A counted day is closed for cash

Once a day has been counted and closed, nothing may move **cash** into or out of it — not a cash
receipt, not a cash bill, not a cash expense, not a drawing. `ICashDayLock` guards every one of
those paths.

The books lock and this are different things and both apply: one freezes a *filed month*, this
freezes a *counted day*. A shop closes a day weeks before it files the month, and without this the
figure the owner counted and signed off can be changed under them afterwards — last night's close
says the till held ₹4,000, a back-dated cash receipt goes in, and the cash book now says ₹4,500 for
a day somebody already agreed.

Two deliberate limits:

- **Only cash.** A credit bill, a UPI receipt or a bank transfer on a closed day changes nothing
  about what was in the drawer, so they go through untouched
- **The latest close in the book, not the last one before the date.** A day nobody closed
  individually still sits inside a counted stretch, because a close carries its opening forward
  through it

## The till cannot hand over notes it does not have

A drawing or a banking that is larger than the drawer holds is refused. Without it the movement is
accepted silently and every close from then on is short by the difference — with the day close
reporting the shortfall as if somebody had taken it.

Money going *in* is never blocked, whatever the till holds.

## A reversal cannot drive stock below zero

Cancelling a purchase, or cancelling a credit note, puts a document into reverse and takes stock
back off the shelf. If the goods have since been sold on there is nothing left to take.

`IStockLedger.EnsureReversible` refuses rather than clamping, because the arithmetic is telling the
truth: goods that were sold really did arrive, so the document being cancelled describes something
that happened. The message names the document and says what to do instead — a debit note if the
goods went back to the supplier, a fresh bill if they went out again.

Every line is checked before any of them moves. A cancel that reversed three lines and then refused
on the fourth would leave the shelf part-way through an undo.

## Period lock

`BooksLockedUpTo` freezes everything dated on or **including** that day. `BooksStartFrom` is the
floor at the other end.

Guarded on **create and cancel** across bills, purchases, credit and debit notes, receipts,
expenses, stock adjustments and day closes. Cancellation checks the **document's own date** — a
March bill cancelled in June changes March.

Refused as a validation error, not a permission error: this is a bar nobody may cross, including
the owner, until the lock moves. Calling it a permission problem would send staff to ask the owner
to do the same forbidden thing. **Only the owner may move the lock, and moving it is logged with
where it came from as well as where it went.**

## The audit trail

Written in the **same transaction** as the thing it describes — a cancellation that succeeded with
no log entry, or a log entry for a cancellation that rolled back, would both be worse than no log.

Recorded: cancellations, stock adjustments, bill discounts, sign-ins, catalogue imports, books
locked and unlocked, shop settings changes (GSTIN, state, name, template), money movements, and
every change to people and roles.

Not recorded: ordinary bills, purchases and receipts. Those are what the registers are for.
