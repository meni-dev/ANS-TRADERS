# ANS Traders

A shop-floor ERP for a **two-wheeler spare parts shop** in Tamil Nadu. Billing, purchases, stock,
money owed both ways, cash, expenses, GST returns and an audit trail — built for one shop, not sold
as a product.

If you have just picked this up and know nothing about it, read this page, then
[`docs/architecture.md`](docs/architecture.md). Everything else is reference.

---

## Running it

Everything runs in Docker. There is no local .NET or Postgres to install.

```bash
cd backend
docker compose up -d          # API on :5266, Postgres on :5432
docker compose exec -T -w /src api dotnet run --project src/Api -- --migrate
docker compose exec -T -w /src/src/Api api dotnet run -- --create-owner   # first time only

cd ../frontend
npm install
npm run dev                   # :5175
```

`--create-owner` prints a generated password **once**. It goes to your terminal, never to a log.
Sign in with it and the app makes you change it before it lets you do anything else.

Every backend command runs inside the container:

```bash
docker compose exec -T -w /src api dotnet build TwoWheelerSpareParts.slnx
docker compose exec -T -w /src api dotnet test TwoWheelerSpareParts.slnx --nologo
docker compose exec -T -w /src api dotnet ef migrations add <Name> \
  --project src/Infrastructure --startup-project src/Api
```

## Checking it still works

Three levels, cheapest first:

```bash
cd backend
docker compose exec -T -w /src api dotnet test TwoWheelerSpareParts.slnx --nologo   # 133 unit tests
./scripts/check-registers.sh                                                        # 45 checks vs live data
./scripts/backup.sh && ./scripts/verify-backup.sh ~/ANS-Traders-Backups/<file>.dump # restore, then verify
```

`check-registers.sh` is the one that matters. It reads every register through the API and asserts
the things a person would assume without being told — that a bill's total is its cash plus its
credit note plus what is still owed, that GSTR-1 and 3B add back to the sales register, that the
stock ledger's closing balance is what the shelf holds. **Run it before handing a quarter to the
accountant, and after any change to how a document settles.**

## Where things are

```
backend/
  src/Domain/          entities and enums. No dependencies on anything.
  src/Application/     services, DTOs, validators, interfaces. The business rules live here.
  src/Infrastructure/  EF Core, repositories, migrations. Talks to Postgres.
  src/Api/             minimal-API endpoint groups, middleware. Thin.
  tests/UnitTests/     134 tests, no database
  scripts/             backup, restore-verify, register checks
  deployment/          Lambda + Cloudflare deployment guide and Worker
frontend/
  src/features/<name>/ types.ts · api.ts · hooks.ts · components/   (one slice per area)
  src/components/      shared UI — form fields, data table, dialogs, layout
  src/lib/             api client, session, formatting, CSV, GST mirror
docs/                  start with architecture.md
```

312 C# files, 143 TypeScript files, 23 migrations.

## The documents

| | |
|---|---|
| [`docs/architecture.md`](docs/architecture.md) | How it is built and the patterns you must follow |
| [`docs/business-rules.md`](docs/business-rules.md) | Every rule the app enforces, and why. **Read before fixing a bug** |
| [`docs/gst.md`](docs/gst.md) | The GST model — what is filed, what is not, how it is worked out |
| [`docs/operations.md`](docs/operations.md) | Deploy, back up, migrate, recover |
| [`docs/decisions.md`](docs/decisions.md) | What was deliberately **not** built, and why. Read before adding anything big |
| [`docs/state-of-play.md`](docs/state-of-play.md) | What is done, what is open, known data issues |
| [`backend/scripts/README.md`](backend/scripts/README.md) | The backup and check scripts |
| [`backend/deployment/README.md`](backend/deployment/README.md) | Lambda, Cloudflare Pages, Neon or Supabase |

## Two things that will save you a day

**The books must always reconcile.** Party balances equal the sum of their ledger entries; stock
equals the sum of its movements; a bill's total equals cash plus credit note plus balance. Nothing
is allowed to break these, and `check-registers.sh` will tell you the moment something does.

**Documents are never edited or deleted.** They are cancelled — they keep their number, keep their
figures, and get compensating ledger rows. A gap in a number series is the first thing an auditor
asks about, so numbers are never reused and never skipped.
