#!/usr/bin/env bash
#
# Takes a compressed dump of the shop's database and drops it somewhere that is not the machine the
# database lives on.
#
# That last part is the whole point. Locally, everything the shop has sits in one Docker volume on
# one laptop; on Neon or Supabase it sits in one account somebody could lock you out of. A dump
# written next to either protects against nothing.
#
# Usage:  ./scripts/backup.sh [destination-directory]
# Cloud:  ANS_DATABASE_URL="postgresql://..." ./scripts/backup.sh
# Cron:   0 21 * * *  cd /path/to/backend && ./scripts/backup.sh >> ~/ANS-Traders-Backups/backup.log 2>&1
#
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$HERE/scripts/_db.sh"

DEST="${1:-${ANS_BACKUP_DIR:-$HOME/ANS-Traders-Backups}}"
KEEP_DAYS="${ANS_BACKUP_KEEP_DAYS:-30}"
FILE="$DEST/ans-traders-$(date +%Y-%m-%d_%H%M).dump"

mkdir -p "$DEST"

echo "$(date '+%F %T')  dumping $(where_am_i)"

# --format=custom, not plain SQL: it restores selectively, it compresses, and pg_restore refuses a
# truncated file outright instead of replaying half a shop and stopping in the middle.
db pg_dump --format=custom --no-owner --no-acl > "$FILE"

SIZE=$(wc -c < "$FILE" | tr -d ' ')

# A zero-byte file is the failure mode that looks like success for months. Refuse it loudly and
# leave nothing behind that could be mistaken for a backup.
if [ "$SIZE" -lt 10000 ]; then
  rm -f "$FILE"
  echo "$(date '+%F %T')  FAILED — dump was only ${SIZE} bytes, so it was deleted rather than kept" >&2
  exit 1
fi

echo "$(date '+%F %T')  ok  $FILE  (${SIZE} bytes)"

# Old dumps are pruned by age, never by count: "keep the last 7" quietly becomes "keep the last 7
# minutes" the day something starts looping.
find "$DEST" -name 'ans-traders-*.dump' -type f -mtime "+$KEEP_DAYS" -delete
