# Shared by the other scripts: decides which database they are talking to.
#
# Local, or Neon, or Supabase — the difference is one environment variable:
#
#   export ANS_DATABASE_URL="postgresql://user:pass@host/db?sslmode=require"
#
# Either way the postgres client runs inside the compose container. That is deliberate: it means a
# laptop needs no psql or pg_dump installed, and the client version is one the project pins rather
# than whatever Homebrew last put there.
#
# One caveat worth knowing: pg_dump refuses to dump a server newer than itself. If the managed
# database is on a later major version than the image, set ANS_PG_IMAGE to one that matches.

ANS_PG_IMAGE="${ANS_PG_IMAGE:-postgres:16}"

# The compose service when running locally; a throwaway container when pointed at a managed
# database, so nothing needs the local stack to be up at all.
db() {
  local tool="$1"; shift

  if [ -n "${ANS_DATABASE_URL:-}" ]; then
    docker run --rm -i "$ANS_PG_IMAGE" "$tool" "$ANS_DATABASE_URL" "$@"
  else
    docker compose --project-directory "$HERE" exec -T postgres \
      "$tool" --username=postgres --dbname=two_wheeler_spare_parts "$@"
  fi
}

where_am_i() {
  if [ -n "${ANS_DATABASE_URL:-}" ]; then
    # Never the password. These lines end up in a cron log.
    echo "$ANS_DATABASE_URL" | sed -E 's|://[^@]*@|://***@|'
  else
    echo "the local compose database"
  fi
}
