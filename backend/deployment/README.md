# Deploying

Three pieces, none of which know about each other beyond a URL:

| Piece | Where | How it gets there |
|---|---|---|
| API | AWS Lambda, behind a Function URL | `dotnet lambda deploy-function` |
| UI | Cloudflare Pages | Pages build from the repo |
| Database | Neon or Supabase | managed |

A Cloudflare Worker puts `api.<your-domain>` in front of the Function URL. There is no API Gateway.

---

## 1. Database

Create the database, then take the **pooled** connection string — not the direct one.

- **Neon** — the endpoint with `-pooler` in the host
- **Supabase** — port **6543**, not 5432

Lambda scales by running more copies of itself, and each copy opens its own pool. Twenty concurrent
invocations against a direct connection will exhaust a small Postgres in seconds. The pooler is what
stands between the shop and that.

Both poolers run PgBouncer in **transaction mode**, which does not keep prepared statements between
statements. Npgsql prepares automatically, so it has to be told not to:

```
Host=<pooled-host>;Port=6543;Database=postgres;Username=<user>;Password=<pass>;
SSL Mode=Require;Trust Server Certificate=true;
Max Auto Prepare=0;No Reset On Close=true;
Maximum Pool Size=2;Timeout=15;Command Timeout=30
```

`Max Auto Prepare=0` is not optional — leave it out and queries fail intermittently, only under
load, with an error about a prepared statement that already exists. `Maximum Pool Size=2` keeps each
Lambda modest, because the number that matters is this multiplied by your concurrency limit.

## 2. Migrations — a deploy step, not a startup step

```bash
cd backend/src/Api
ConnectionStrings__Default="<connection string>" dotnet run -- --migrate
```

Never on the way up. Several cold Lambdas start at once and would race each other through the same
migration, and a schema change that fails should stop a deploy rather than take a running shop down.

Use the **direct** connection string here, not the pooled one — DDL and transaction pooling do not
mix well.

## 3. API on Lambda

The app already turns itself into a Lambda handler when it detects one, and behaves normally when it
does not, so there is nothing to switch on.

```bash
cd backend/src/Api
dotnet tool install -g Amazon.Lambda.Tools   # once
dotnet lambda deploy-function
```

Everything it needs is in `aws-lambda-tools-defaults.json` next to the project, so the command takes
no flags. Two of those settings are worth understanding rather than copying.

### Why `provided.al2023` and not `dotnet10`

AWS's managed .NET runtimes follow the LTS releases and lag behind them. This project targets
**net10.0**, so rather than wait for a managed runtime to appear, the app ships its own copy of .NET
inside the deployment bundle — that is what `--self-contained true` in `msbuild-parameters` does, and
it is the supported way to run a .NET version Lambda does not manage.

**This is not what a Lambda layer is for.** Layers are how Node and Python projects share
dependencies between functions; a .NET deployment already carries everything it needs in one bundle,
so there is no layer here and nothing missing by its absence.

Check whether a managed `dotnet10` runtime exists in your region before you deploy. If it does, you
can drop `--self-contained true` and set `"function-runtime": "dotnet10"` with the handler back to
`Api::Api.LambdaEntryPoint::FunctionHandlerAsync` — smaller bundle, faster cold start, but AWS
patches the runtime on their schedule rather than yours.

### Memory and auth

**Memory 1024MB or more.** Lambda gives CPU in proportion to memory, and sign-in runs PBKDF2 with
600,000 iterations — at 256MB that is a visible pause before the first bill of the day.

**Auth type NONE** is correct. The function is public in the sense that anything can reach it, and
then every request that is not `/api/auth/sign-in` or `/health` is refused by the app's own session
check. That is the same guard that runs locally.

### If the self-contained bundle gives trouble

The other way that definitely works for a .NET version Lambda does not manage is a **container image
function** — build on the `mcr.microsoft.com/dotnet/aspnet:10.0` base with the Lambda Runtime
Interface Client, push to ECR, point the function at it. Heavier to set up, and it removes the
250MB unzipped bundle limit entirely.

Environment variables:

```
ASPNETCORE_ENVIRONMENT      = Production
ConnectionStrings__Default  = <the pooled connection string>
Cors__AllowedOrigins__0     = https://<your-pages-domain>
Shop__TimeZone              = Asia/Kolkata
```

`Cors__AllowedOrigins__0` has to be the exact origin the browser sends — scheme and host, no
trailing slash. Left empty, every call from the UI is blocked and it looks exactly like the API
being down. The app logs a warning at startup when it is empty.

`Shop__TimeZone` decides what "today" means. Lambda runs on UTC, so without it a bill written before
half past five in the morning would be dated to the previous day.

### Creating the first account

```bash
ConnectionStrings__Default="<connection string>" dotnet run -- --create-owner
```

Prints a generated password once, to your terminal. It is deliberately not written to the log —
CloudWatch keeps log lines for as long as retention says, and that is not where a password belongs.

## 4. The Worker in front of the Function URL

A Function URL cannot take a custom domain by itself, and a proxied CNAME does not work either: AWS
decides which function you meant from the `Host` header, so a request arriving as
`api.your-domain.com` comes back 403. The Worker in `cloudflare/` rebuilds the request against the
real function hostname and passes everything else through.

```bash
cd backend/deployment/cloudflare
# put your Function URL in wrangler.toml, and your domain in the route
npx wrangler deploy
```

## 5. UI on Cloudflare Pages

| Setting | Value |
|---|---|
| Build command | `npm run build` |
| Output directory | `dist` |
| Root directory | `frontend` |
| Environment variable | `VITE_API_BASE_URL = https://api.<your-domain>` |

`VITE_API_BASE_URL` is read **at build time**, not at run time — changing it needs a rebuild, not a
restart.

`public/_redirects` sends every path to `index.html`, without which the app works until somebody
refreshes the page they are on. `public/_headers` keeps the shell uncached and the hashed assets
cached forever.

## 6. Backups

The managed database has its own snapshots, and they are not yours — they live in the same account
somebody could lose access to. Take your own as well:

```bash
export ANS_DATABASE_URL="<connection string>"
./scripts/backup.sh
./scripts/verify-backup.sh ~/ANS-Traders-Backups/<file>.dump
```

The verify step restores into the local Docker Postgres, whatever the dump came from — which is the
stronger test, because it proves the dump can be brought back somewhere other than where it was
made. See `scripts/README.md`.

---

## Limits worth knowing before you hit them

**A Function URL response is capped at 6MB.** Registers are deliberately not paged — half of March
is worse than none of it — so a year of stock movements or a big sales register will grow past that.
It is fine at this shop's size today. When it stops being fine, the answer is to write the export to
S3 and hand back a link, not to page the register.

**Catalogue import is one request.** Five thousand rows are validated and written together, all or
nothing. Set the function timeout high enough (60s is a reasonable start) and keep an eye on it.

**A pooled connection and a cold Lambda both take a moment.** Neon's free tier suspends after
inactivity; the first bill of the morning can take a few seconds while the database wakes and the
function starts. Neither is broken.
