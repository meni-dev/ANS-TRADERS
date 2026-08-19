# Backup

Everything the shop has — every bill, every balance, every part — lives in one Docker volume on one
machine. If that machine is stolen or its disk dies, the shop has nothing. These two scripts are
what stands between the shop and that.

## Taking a backup

```bash
cd backend
./scripts/backup.sh                      # writes to ~/ANS-Traders-Backups
./scripts/backup.sh /Volumes/Backup/ans  # or somewhere else
```

Against a managed database — Neon, Supabase — point it there instead:

```bash
export ANS_DATABASE_URL="postgresql://user:pass@host/db?sslmode=require"
./scripts/backup.sh
```

The postgres client runs inside a container either way, so nothing needs `psql` installed. If the
managed database is on a later major version than `postgres:16`, set `ANS_PG_IMAGE` to match —
`pg_dump` refuses to dump a server newer than itself.

Write it somewhere that is **not this laptop** — an external disk, a network share, a synced folder.
A dump sitting next to the database it came from protects against nothing except a bad migration.

Run it nightly, after the shop closes:

```
0 21 * * *  cd /Users/you/ANS-TRADERS/backend && ./scripts/backup.sh >> ~/ANS-Traders-Backups/backup.log 2>&1
```

Dumps older than 30 days are removed automatically. Change that with `ANS_BACKUP_KEEP_DAYS`.

A dump that comes out suspiciously small is deleted and the script exits non-zero, so a cron job
that has quietly been writing empty files shows up in the log instead of the day it is needed.

## Proving the backup works

```bash
./scripts/verify-backup.sh ~/ANS-Traders-Backups/ans-traders-2026-08-19_2100.dump
```

It restores the dump into a scratch database, checks that the books still add up inside the restored
copy — party balances against their ledgers, stock against its movements, invoices balancing three
ways, allocations against their payments — and then drops the scratch database.

**Run this at least once**, and again after any migration. A backup nobody has restored is a file,
not a backup; the only way to know a dump is good is to bring it back.

# Checking the registers

```bash
./scripts/check-registers.sh                        # everything, all dates
./scripts/check-registers.sh 2026-04-01 2027-03-31  # one financial year
```

Reads every register through the API and asserts the things a person would assume without being
told — that a bill's total is its cash plus its credit note plus what is still owed, that GSTR-1 and
GSTR-3B add back to the sales register, that the stock ledger's closing balance is what the shelf
holds. Forty-odd checks; it exits non-zero on the first thing that does not agree.

Worth running after any change to how a document settles, and before handing a quarter to the
accountant. Two of the checks in it exist because those figures had already gone out wrong once.
