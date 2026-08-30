# Operations

Running it, backing it up, migrating it, and getting it back when something goes wrong.

---

## Migrations

**A deploy step, never something the app does on the way up.** Several copies start at once on
Lambda and would race each other through the same migration; and a schema change that fails should
stop a deploy, not take a running shop down with it.

```bash
cd backend/src/Api
ConnectionStrings__Default="<connection string>" dotnet run -- --migrate
```

Locally the same command runs inside the container. Use the **direct** connection string, not a
pooled one — DDL and transaction pooling do not mix.

23 migrations as of August 2026. Two of them carry data as well as schema and are worth knowing
about, because both would have broken a live shop if written the way the scaffolder wrote them:

- **`AddRolesAndPermissions`** seeds the two roles and moves every existing account onto one, inside
  the same transaction as the column that requires them. Anything not explicitly Owner became
  Counter Staff — erring towards less, because a person who finds a screen missing says so within
  the hour while one quietly handed the cancel button says nothing at all.
- **`AddDocumentCounters`** seeds each counter from `MAX(Sequence)` per series per year, so the
  first document after it continued at 0057 rather than restarting at 1.

## The first account

```bash
cd backend/src/Api
ConnectionStrings__Default="<connection string>" dotnet run -- --create-owner
```

Prints a generated password **once**, to the terminal — never to a log, because CloudWatch keeps log
lines for as long as retention says and that is not where a password belongs. The account is forced
to change it on first sign-in.

## Backups

The managed database has its own snapshots and they are **not yours** — they live in the same
account somebody could lose access to.

```bash
cd backend
export ANS_DATABASE_URL="postgresql://user:pass@host/db?sslmode=require"   # omit for local
./scripts/backup.sh                          # writes to ~/ANS-Traders-Backups
./scripts/verify-backup.sh ~/ANS-Traders-Backups/<file>.dump
```

Nightly, after the shop closes:

```
0 21 * * *  cd /path/to/backend && ./scripts/backup.sh >> ~/ANS-Traders-Backups/backup.log 2>&1
```

Two behaviours that matter: a suspiciously small dump is **deleted** and the script exits non-zero,
because a cron job quietly writing empty files is the failure that looks like success for months;
and old dumps are pruned by **age**, never by count, because "keep the last 7" quietly becomes "keep
the last 7 minutes" the day something loops.

`verify-backup.sh` restores into the **local** Docker Postgres whatever the dump came from, then
checks the books add up inside the restored copy — party balances against their ledgers, stock
against its movements, invoices balancing three ways, allocations against their payments, that the
migration history came across, and that at least one account can still sign in. That last one
matters: a dump with no accounts restores cleanly and locks you out.

**A backup nobody has restored is a file, not a backup.** Run the verify at least once, and again
after any migration.

## Checking the books

```bash
./scripts/check-registers.sh                        # every date
./scripts/check-registers.sh 2026-04-01 2027-03-31  # one financial year
```

48 checks. Exits non-zero on the first thing that does not agree. Two of them exist because those
figures had already gone out wrong once.

## Checking what the app refuses

```bash
./scripts/check-negatives.sh
```

48 cases — every way somebody could try to make the books say something untrue, and the app's
answer: negative quantities, selling what is not there, taking back more than went out, paying a
settled bill, emptying a till that is already empty, cash into a closed day, a GST rate that is not
a slab, and six counters billing the last unit at the same moment.

Each case states what *should* happen, so a case that behaves differently is a hole rather than a
preference. It builds its own parts and parties, uses them, and removes every trace on the way out —
safe to run against a live shop.

Run it after any change to a service that moves stock or money.

It mints a short-lived session directly in the database, runs, and removes it — it is a local
diagnostic on the shop's own machine and must not become a way to hold a token around.

Three of the checks do not read a register at all: they assert that the carried totals still agree
with the ledgers behind them, which is the one thing the registers cannot catch, because every
register agrees happily with the same wrong number. That gap was real — two party balances had
drifted and 45 register checks passed anyway.

Below the pass line it prints a **worth knowing** section. That is where a shelf that once went
negative is reported: the books are consistent, but a bill is dated before its goods arrived. It is
a data correction only the shop can make, so it is surfaced rather than turned into a red light
somebody would switch off.

## Checking the data itself

```bash
./scripts/check-data.sh
```

A different question from the other two. `check-registers.sh` asks whether the app agrees with
itself; `check-negatives.sh` asks what it refuses; this one reads the figures somebody typed in and
says which of them will cause trouble — a part with no ceiling price, a GSTIN whose check digit is
wrong, a hole in a number series, a cheque that has been sitting in a drawer for a month.

Fourteen kinds of finding across the catalogue, the parties, the documents and the money. Each one
says how many and **why it matters**, because a list of anomalies with no consequence attached gets
read once.

Nothing in it is a defect and nothing in it fails the run. Read it before a return goes out.

## Deployment

See [`../backend/deployment/README.md`](../backend/deployment/README.md) for the full route:
**API on Lambda behind a Function URL**, **UI on Cloudflare Pages**, **database on Neon or
Supabase**, with a Cloudflare Worker mapping `api.<domain>` onto the Function URL.

Four things there that are easy to get wrong and expensive to debug:

1. Use the **pooled** connection string, with `Max Auto Prepare=0` — the poolers run PgBouncer in
   transaction mode and Npgsql's automatic prepared statements break against it, intermittently and
   only under load
2. `Cors__AllowedOrigins__0` must be the exact origin, and CORS must **not** also be set at the
   gateway or every response carries the header twice
3. `Shop__TimeZone=Asia/Kolkata` — without it, a bill written before half past five in the morning
   is dated to the previous day
4. Memory **1024MB or more** — Lambda gives CPU in proportion, and sign-in runs PBKDF2 600,000 times

## When something goes wrong

**Figures disagree** → `./scripts/check-registers.sh`. It names the register and the two numbers.

**Two people billing at once fails** → should not happen; see the document numbering section in
[`architecture.md`](architecture.md). If it does, look for a new create path that skipped
`IUnitOfWork.InTransactionAsync`.

**A date is a day out** → something used `DateTime.Today` instead of `IShopClock`.

**A permission is enforced but invisible on the roles screen** → it is missing from
`PermissionCatalogue`. A test catches this.

**Nobody can sign in** → the account may be locked (5 wrong passwords). Clear it by resetting the
password from Settings → People, or directly:
`UPDATE users SET "FailedSignInCount" = 0, "LockedOutUntil" = NULL WHERE "Username" = '…';`

**A restore is needed** → the newest verified dump in `~/ANS-Traders-Backups`, restored with
`pg_restore --no-owner --no-acl --exit-on-error`. Never accept a partial restore: it looks like a
working database and is missing rows.
