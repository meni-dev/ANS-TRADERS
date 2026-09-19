# Infra checklist

Tracks what's actually been set up, as opposed to `README.md` which explains how. Tick items off
as they're done; fill in the blanks as accounts get created. Nothing here is a secret itself —
actual keys and passwords go in a password manager and in GitHub Secrets, never in this file.

## Accounts

- [ ] **Neon** — project created, region picked
- [ ] **AWS** — account ready, IAM deploy user created with Lambda permissions
- [ ] **Cloudflare** — account ready, domain added and DNS live on Cloudflare
- [ ] **GitHub** — `production` environment created with a required reviewer set

## Values to fill in once known

| Item | Value |
|---|---|
| Domain | |
| API subdomain | `api.<domain>` |
| Neon pooled connection string | |
| Neon direct connection string | |
| AWS region | |
| Cloudflare account ID | |
| Cloudflare API token (Pages:Edit) | created, stored in password manager — not here |
| Lambda Function URL | |

## GitHub Secrets — Settings → Secrets and variables → Actions → Secrets

- [ ] `AWS_ACCESS_KEY_ID`
- [ ] `AWS_SECRET_ACCESS_KEY`
- [ ] `DB_CONNECTION_DIRECT`
- [ ] `DB_CONNECTION_POOLED`
- [ ] `CLOUDFLARE_API_TOKEN`
- [ ] `CLOUDFLARE_ACCOUNT_ID`

## GitHub Variables — same page, Variables tab

- [ ] `AWS_REGION`
- [ ] `CF_PAGES_PROJECT`
- [ ] `VITE_API_BASE_URL`
- [ ] `API_URL`
- [ ] `APP_URL`

## One-time manual steps (not run by the workflow)

- [ ] Migration run once against the direct connection (`--migrate`), before the first deploy
- [ ] Owner account created (`--create-owner`) — password saved somewhere real, it's shown once
- [ ] Lambda environment variables set in the console: `ConnectionStrings__Default`,
      `Cors__AllowedOrigins__0`, `Shop__TimeZone`
- [ ] Cloudflare Worker deployed in front of the Function URL (`cloudflare/`, `wrangler deploy`)
- [ ] Cloudflare Pages project connected to the repo, build settings set per `README.md` §5

## First deploy, in order

1. [ ] Database created, pooled + direct connection strings in hand
2. [ ] Migration run
3. [ ] Owner account created, password saved
4. [ ] Lambda function deployed, environment variables set
5. [ ] Worker deployed in front of the Function URL
6. [ ] Pages project connected and building
7. [ ] GitHub Secrets and Variables filled in
8. [ ] `production` environment reviewer set
9. [ ] Push to `main`, approve the deploy, watch `check-registers.sh` pass
