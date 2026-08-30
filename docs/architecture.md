# Architecture

.NET 10 minimal APIs, React 19 + MUI, PostgreSQL 16. Everything in Docker.

```
Api  ──▶  Application  ──▶  Domain
 │            │
 └────────────┴──▶  Infrastructure  ──▶  Postgres
```

`Domain` depends on nothing. `Application` holds the rules and talks to the database only through
interfaces it defines. `Infrastructure` implements those interfaces. `Api` is thin — it parses a
request, calls one service, and returns.

Where a rule goes: **in the service**, next to the data it reads. Shape-only checks (is this field
present, is this a number, is this in range) go in a FluentValidation validator. Anything needing
another row — "is there enough stock", "is this past the credit limit" — belongs in the service,
because the validator cannot see the database.

---

## The five patterns

Copy these. Do not invent a sixth shape for the same problem.

### 1. The ledger

Every running total in this system has the same shape:

- an **append-only ledger table** — every movement, never edited
- a **denormalised total** on the entity — fast to read
- **one interface that is the sole writer, and that does not save** — the caller saves, so the
  document, its ledger rows and the balance all commit in one transaction

Three of them: `IStockLedger` (stock), `IPartyLedger` (what a party owes), `IPaymentLedger`
(payments and allocations). `IAuditLog` follows the same non-saving rule.

Two traps, both hit in practice:

- **A stored `BalanceAfter` is not a running balance.** It records the total when the row was
  *written*. A statement reads in the order things *happened*, so any backdated entry makes the
  column stop adding up down the page. Statements compute the running figure from a carried-in
  balance instead.
- **`Domain.Common.Entity` sets `Id` in its initialiser**, so EF's "the key is still default,
  therefore this is new" test fails for a child added to an already-tracked parent — it stages an
  UPDATE against a row that does not exist. Child rows go in through the repository explicitly.

Anything new that accumulates copies this. And add its drift count to
`DashboardRepository.CheckReconciliationAsync`, which surfaces on the dashboard as
*Balances reconcile*.

### 2. Snapshot, never reference

A document copies what it needs at the moment it is raised, and never looks it up again.

`Invoice.CustomerName`, `InvoiceItem.GstRate`, `InvoiceItem.CostRate`, `InvoiceItem.SupplyType`,
`PaymentAllocation.DocumentNumber` — all snapshots. Rename a customer and last year's bill still
shows the name it was issued under. Reclassify a part and a filed return does not move.

`CostRate` is `decimal?` on purpose: a line sold before costing existed has no honest value, and
zero would read as "these goods were free" — a 100% margin on every historical bill.

### 3. Cancel, never edit or delete

A document that was wrong is cancelled. It keeps its number, keeps its figures, gets compensating
ledger rows, and its status changes. Nothing is ever deleted.

On a cancelled document `BalanceDue` goes to zero but `AmountPaid` is left alone — it is the record
that money really did cross the counter. Every reconciliation query therefore filters on **status**,
never on an amount being zero.

### 4. Say so when you cannot know

Where a figure is genuinely unknown, the app reports *not known* rather than a number that looks
right. Zero is a claim, and on a money screen it is usually a false one.

`InvoiceItem.CostRate` null · `ProfitAndLossDto.CostCoveragePercent` · `RateDriftRowDto.MarginPercent`
null for an unpriced part · `ReorderRowDto.DaysOfCover` null when nothing is moving ·
`ProductDto.PurchaseRate` null for somebody without `CostView`.

Before defaulting anything to zero, ask whether zero is a fact or a guess.

### 5. Permissions are code, roles are data

`Domain.Enums.Permission` is a fixed list — 22 of them — and the shop cannot add to it. A permission
is only real because some service refuses to run without it; an invented one would be a row that
looks like protection and stops nothing.

Roles are rows the shop builds and names. `Role.IsSystem` marks the built-in one: it holds
everything and refuses to be edited or deleted, because one wrong tick could otherwise leave a shop
where nobody can add a user or unlock the books.

- **Writes are guarded almost everywhere.**
- **Reads are guarded only where they expose something**: `PurchaseView` (a purchase bill *is* the
  buying price), `CostView` (P&L, valuation, dead stock, rate drift, dashboard purchase totals, and
  `ProductDto.PurchaseRate`), `ReportView` (registers, GST, the dashboard GST panel), `AuditView`,
  `UserManage`, and the money summaries (`/api/expenses`, `/api/cash/*`, `/api/payments/dues`,
  `/api/payments/summary`, `/api/stock/losses`).
- **Changing a role drops that person's sessions** — permissions are copied onto `ICurrentUser` when
  the request starts, so a demotion would otherwise not bite until they signed out.
- Frontend gating (`useAuth().can`, `RequirePermission`, `visibleNavItems`) is about not offering
  doors that will not open. **A hidden button is not a lock.**

`PermissionCatalogue` must describe every enum member — a test enforces it, because an undescribed
permission is enforced by code and invisible on the roles screen, so nobody could ever grant it.

---

## Things that will bite you

### Document numbering

**Never** compute the next number as `MAX(Sequence) + 1`. That was a real bug: five simultaneous
bills produced one invoice and four `HTTP 500`s from a unique-index violation.

Numbers come from `IDocumentNumbers.NextAsync`, which does one
`INSERT … ON CONFLICT DO UPDATE … RETURNING` against `document_counters`. The statement locks the
row until the caller commits, so concurrent requests queue instead of colliding.

The claim **must** be inside the document's own transaction — every create path is wrapped in
`IUnitOfWork.InTransactionAsync` for exactly this reason. Without it a failed bill burns its number
and leaves a gap in the series.

> Invoice numbers **0060–0062 do not exist**. They were consumed by the first version of this fix,
> before the claim was made transactional. The dashboard's numbering check reports them, correctly.

### Time

The server runs on UTC — it did in Docker and it does on Lambda. **Never use `DateTime.Today`.**
Ask `IShopClock`, which resolves `Shop:TimeZone` (default `Asia/Kolkata`).

Instants stay UTC: `CreatedAt`, an audit row's `OccurredAt`, a session's expiry. Those are moments,
not days, and moments have no timezone problem. Only *calendar day* decisions use the shop clock.

### Concurrency

`xmin` is the row version — Npgsql-native, no schema column. A conflict surfaces as
`DbUpdateConcurrencyException` and is mapped to `409` with a message telling the user to reload. A
raw unique violation (SQLSTATE 23505) is mapped to `409` too, as a net.

### Enums in the database

Stored **by name**, never by number — `HasConversion<string>()`. Inserting a member into an enum
must not silently reclassify existing rows. If you add an enum that reaches the database, do this.

---

## Frontend conventions

One slice per area: `features/<name>/{types.ts, api.ts, hooks.ts, components/}`. No barrel files.

- `types.ts` mirrors the backend DTOs, plus zod schemas for forms
- `api.ts` calls `apiRequest` from `lib/api/client`
- `hooks.ts` wraps TanStack Query
- Forms are react-hook-form + zod through the shared `RHF*` field components

The session token lives in `localStorage` and travels as a bearer header — not a cookie. That is
what makes the cross-origin Cloudflare-to-Lambda split work without cookie trouble; the trade is
that a script on the page could read it.

`lib/documents/gst.ts` **mirrors** the server's GST arithmetic so the counter screen can show a
total before saving. The server always recomputes and is authoritative. If you change one, change
both — and note the tolerance in `InvoiceService`, which allows a rupee between the two on the
tendered amount because round-off can differ by a paisa.

MUI notes that have caught people: `alignItems`/`justifyContent` go in `sx`, not as `Stack` props;
`RHFSelectField` takes `options`, not children; `ConfirmDialog` uses `description`/`onCancel`;
`StatTile` uses `caption`; `DataTable` has no `getRowId`.

**The shell.** `components/layout/` owns navigation and nothing else does:

- `navConfig.tsx` is the single source of what exists. Rows, permissions, the active-route match,
  the breadcrumb trail and the flat page list the command palette searches all come from it. Adding
  a screen means adding a row here, not wiring three places
- `quickActions.tsx` is the day's five jobs. The dashboard strip, the app bar's New button and ⌘K
  all read this one list — the reason they cannot disagree about which actions exist or who may see
  them
- The sidebar collapses to a 60px rail, remembered in `localStorage` under `ans.sidebar`. Collapsed,
  a section's children live in a hover flyout; **that flyout is load-bearing, not decoration** —
  without it half the app is unreachable in rail mode
- ⌘K opens the palette, ⌘\ toggles the sidebar. Both are registered once, in `AppLayout`

**Dashboard modules wear one shell.** `components/data/PanelCard` supplies the header, the caption,
the optional footer link and `height: 100%`. Panels differ in what they show, never in how they are
framed, and two modules in a grid row end level because the shell stretches rather than because
their contents happened to match.

**Errors are written for the counter, and there is one path to them.** `lib/api/errors.ts`
`describeError(error, fallback)` is the only thing a screen calls; nothing reads `error.message`
directly. That rule exists because reading it is the obvious thing to do and it was wrong
everywhere: a validation response puts its useful text in `errors` and the word **"Validation
failed"** in `message`, so every form in the app used to answer a shop owner with a developer's
placeholder while the sentence that would have helped sat one field away.

The order is field objection, then a known code, then the server's own sentence, then a fallback —
and a network failure is turned into an `ApiError` in the client so "the shop is offline" travels
the same path as everything else.

Concurrency messages come from the **server**, keyed off the request route rather than the clashing
row: EF reports only the row whose version moved, and for a save that touches several that is
usually the party balance — so cancelling a bill would have announced that "an account changed".
Every one of them says the same three things in the same order: what happened, that nothing was
recorded twice, and what to do.

**Every screen opens with `PageHeader`.** Icon chip, title, optional count badge, caption, actions.
Two dozen pages had each hand-rolled the same band and had already drifted on spacing and on where
the count chip sat. Two props exist because a header is not always a header: `align="flex-end"` for
a row whose right-hand side is a labelled date filter rather than buttons, and `className` so a
document screen can pass `no-print` and keep its buttons off the paper.

**Two marks, on purpose.** `BrandMark` is a vector redraw of the shop's logo — gear plus rising
arrow, the same two colours — used in the app chrome and on the sign-in card, where a 354px PNG
went soft at 28px. `ShopLogo` still serves the supplied file, and it is what the five invoice
templates print: the bill carries the shop's own artwork, not a redraw of it.

**Colour on the dashboard labels, it does not grade.** `theme.accent` holds five tint/solid pairs
for the icon chips on `StatTile` and `PanelCard`. They say *which card this is*, nothing more — the
hue on the tiles follows the direction of the money (blue what was sold, violet the month behind,
amber owed to the shop, rose owed by it) so it is at least consistent, but a figure's own colour
still comes from the semantic palette. The one place colour genuinely means something is the
month-on-month pill, where a fall is red because a fall is the thing worth noticing.

**Input labels sit above the box, not in the border.** The theme forces `shrink` on every
`InputLabel`, makes it `position: static` so it flows above the field, and hides the notch
(`MuiOutlinedInput.notchedOutline`). MUI's floating label is positioned by a hardcoded
`translate(14px, -9px)` calibrated for a 16px label; at the 14px this theme uses, the border line
crossed the tops of the letters on every field in the app. Two consequences worth knowing:

- Never pass `shrink` at a call site — it is the default now, and a stray `shrink: false` would put
  one field back inside its box
- A labelled field is ~21px taller than the box alone, so a row that mixes fields with buttons
  wants `alignItems: 'flex-end'`, not `'center'`, or the button floats below the box
