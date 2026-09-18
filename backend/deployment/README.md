# Deploying

Three pieces, none of which know about each other beyond a URL:

| Piece | Where | How it gets there |
|---|---|---|
| API | AWS Lambda, behind a Function URL | `dotnet lambda deploy-function` |
| UI | Cloudflare Pages | Pages build from the repo |
| Database | Postgres on our own EC2 box | see below |

A Cloudflare Worker puts `api.<your-domain>` in front of the Function URL. There is no API Gateway.

The database is self-hosted rather than a managed Neon/Supabase instance — full control, no
free-tier cold-start-after-inactivity, and no third account holding the shop's data. The trade is
real: patching, backups and connection pooling are now ours to run, not someone else's. Sections 1
and 3 below cover what that costs in setup; section 6 covers what it costs in ongoing care.

---

## 1. Database — Postgres on EC2

### Why the instance has no public IP

Nothing about this box needs to be reachable from the open internet, and the moment it is, it is
someone else's problem too. Instead, the EC2 instance and the Lambda function sit in the **same
VPC**, talking over private IPs only:

- The DB's security group allows inbound 5432/6432 **only from the Lambda function's security
  group** — not from `0.0.0.0/0`, not from "my IP."
- Lambda gets no internet access this way (no NAT gateway means no outbound route), which is fine:
  this API never calls anything external — no email, no SMS, no third-party API. If that ever
  changes, a NAT gateway is the fix, not opening the database up.
- Nobody SSHes in either. The instance gets an IAM role with `AmazonSSMManagedInstanceCore` and is
  reached through **AWS Systems Manager Session Manager** — no key pair to lose, no port 22 open,
  and every session is logged in CloudTrail.

### Launch it

- **AMI**: Ubuntu 24.04 LTS, arm64 (matches the Lambda function's Graviton architecture — no
  connection to performance, just one fewer thing to keep track of)
- **Instance type**: `t4g.small` (2 vCPU, 2GB). Postgres and PgBouncer for one shop's traffic do not
  need more; `t4g.micro`'s 1GB is too tight once the OS and both services are running at once
- **Storage**: 20GB gp3 to start — a spare-parts shop's transaction history is small; resize later
  if it ever matters
- **Network**: your default VPC is fine. Auto-assign public IP → **disabled**
- **IAM role**: a new role with `AmazonSSMManagedInstanceCore` attached, so Session Manager works
- **Security group** (`ans-db-sg`): no inbound rules yet — add the Lambda rule once that security
  group exists in section 3

### Install Postgres and PgBouncer

Connect with `aws ssm start-session --target <instance-id>`, then:

```bash
sudo apt update
sudo apt install -y postgresql postgresql-contrib pgbouncer docker.io awscli

sudo -u postgres createuser --pwprompt ans_app         # the app's own login, not postgres itself
sudo -u postgres createdb --owner=ans_app two_wheeler_spare_parts
```

Postgres listens on the private network only — edit `/etc/postgresql/16/main/postgresql.conf`:

```
listen_addresses = 'localhost'   # PgBouncer is the only thing that talks to Postgres directly
```

PgBouncer is what Lambda actually connects to, in **transaction pooling mode** — the same reason
Neon and Supabase front their own Postgres with a pooler: Lambda scales by running more copies of
itself, and each copy opens its own connection. Twenty concurrent invocations against Postgres
directly will exhaust it in seconds; PgBouncer is what stands between the shop and that.
`/etc/pgbouncer/pgbouncer.ini`:

```ini
[databases]
two_wheeler_spare_parts = host=127.0.0.1 port=5432 dbname=two_wheeler_spare_parts

[pgbouncer]
listen_addr = 0.0.0.0
listen_port = 6432
auth_type = scram-sha-256
auth_file = /etc/pgbouncer/userlist.txt
pool_mode = transaction
max_client_conn = 200
default_pool_size = 10
```

```bash
echo "\"ans_app\" \"<the password you set above>\"" | sudo tee /etc/pgbouncer/userlist.txt
sudo systemctl enable --now postgresql pgbouncer
```

### The connection string

Points at PgBouncer's port (6432), on the instance's **private** IP — nothing outside the VPC can
reach it regardless:

```
Host=<ec2-private-ip>;Port=6432;Database=two_wheeler_spare_parts;Username=ans_app;Password=<pass>;
SSL Mode=Disable;
Max Auto Prepare=0;No Reset On Close=true;
Maximum Pool Size=2;Timeout=15;Command Timeout=30
```

`SSL Mode=Disable` is deliberate here and would not be on a managed database: the traffic never
leaves AWS's private network, so TLS is buying nothing a stricter security group is not already
buying. `Max Auto Prepare=0` still is not optional — PgBouncer's transaction mode does not keep
prepared statements between statements, and Npgsql prepares automatically unless told not to.

## 2. Migrations — a deploy step, not a startup step

The instance has no public IP, so a laptop cannot reach port 6432 directly. Open a tunnel through
Session Manager first — this needs nothing on the instance beyond the SSM agent already installed
as part of Ubuntu, and nothing on the laptop beyond the AWS CLI:

```bash
aws ssm start-session \
  --target <instance-id> \
  --document-name AWS-StartPortForwardingSession \
  --parameters '{"portNumber":["5432"],"localPortNumber":["5432"]}'
```

Leave that running in its own terminal, then migrate against `localhost` in another one:

```bash
cd backend/src/Api
ConnectionStrings__Default="Host=localhost;Port=5432;Database=two_wheeler_spare_parts;Username=ans_app;Password=<pass>;SSL Mode=Disable" \
  dotnet run -- --migrate
```

Never on the way up, and never through PgBouncer — both for the same reason: several cold Lambdas
starting at once would race each other through the same migration under pooling, and DDL through a
transaction-mode pooler is exactly the mix PgBouncer's docs warn against. Tunnel straight to
Postgres's own 5432, not PgBouncer's 6432, and run it as one deliberate step so a schema change that
fails stops the deploy instead of taking a running shop down.

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

### Putting the function in the database's VPC

This is what makes the private-IP-only database in section 1 reachable at all. Create a second
security group, `ans-lambda-sg`, then go back and add one inbound rule to `ans-db-sg`: **5432/6432
from `ans-lambda-sg`**. Nothing else needs to reach the database, so nothing else gets a rule.

`aws-lambda-tools-defaults.json` does not carry the subnet and security group IDs — they are
specific to this one AWS account, not something to commit for whoever else ever reads this repo.
Add them once, after creating `ans-lambda-sg`:

```json
"function-vpc-subnet-ids": "<subnet-id>,<subnet-id>",
"function-vpc-security-group-ids": "<ans-lambda-sg-id>"
```

Use the same subnets the EC2 instance is in. A Lambda function placed in a VPC loses its default
internet route unless a NAT gateway is added — that is not needed here, since this API never calls
anything outside the VPC, but it does mean a NAT gateway is the fix on the day that stops being
true, not a reason to skip the VPC in the meantime.

### Why the managed `dotnet10` runtime

This used to deploy onto `provided.al2023` carrying its own copy of .NET, because when it was
written net10.0 had no managed runtime to land on. It has one now — Amazon Linux 2023, deprecation
November 2028, on both architectures — and moving to it is worth doing for a reason that has nothing
to do with tidiness:

| Build | Unzipped | Zipped |
|---|---|---|
| self-contained + ReadyToRun | 144MB | **54MB** |
| self-contained, no ReadyToRun | 126MB | 47.9MB |
| **framework-dependent + ReadyToRun** | 31MB | **10.2MB** |
| framework-dependent, no ReadyToRun | 12MB | 4.2MB |

**Lambda refuses a direct upload over 50MB.** The self-contained bundle was over it, so every deploy
had to route through an S3 bucket — a bucket, a policy and a flag to keep alive for no gain.
Dropping ReadyToRun would have squeezed under at 47.9MB, with 2MB of headroom and a worse cold
start. Framework-dependent removes the question.

The trade is that AWS patches a managed runtime on their schedule rather than yours. Against a fifth
of the bundle size, shorter cold starts and one less moving part, it is the better side of the deal
for one shop.

**The handler does not change.** The app uses top-level statements with `AddAWSLambdaHosting`, which
is the executable-assembly model, and AWS supports that model on the managed runtimes — the handler
stays the bare assembly name `Api`. It does **not** need `Assembly::Type::Method`, and there is no
`LambdaEntryPoint` class in this project to point at. (An earlier version of this page said
otherwise. It was wrong.)

ReadyToRun stays on. Framework-dependent, it pre-compiles this app's own assemblies rather than the
shared framework — 6MB for taking the JIT off the cold path, which is what the first sign-in of the
day feels.

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
ConnectionStrings__Default  = <the PgBouncer connection string from section 1>
Cors__AllowedOrigins__0     = https://<your-pages-domain>
Shop__TimeZone              = Asia/Kolkata
```

`Cors__AllowedOrigins__0` has to be the exact origin the browser sends — scheme and host, no
trailing slash. Left empty, every call from the UI is blocked and it looks exactly like the API
being down. The app logs a warning at startup when it is empty.

`Shop__TimeZone` decides what "today" means. Lambda runs on UTC, so without it a bill written before
half past five in the morning would be dated to the previous day.

### Creating the first account

Through the same SSM tunnel as migrations (section 2):

```bash
ConnectionStrings__Default="Host=localhost;Port=5432;Database=two_wheeler_spare_parts;Username=ans_app;Password=<pass>;SSL Mode=Disable" \
  dotnet run -- --create-owner
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

A managed database comes with its own snapshots, restorable from the same provider's dashboard even
if the shop's own backup ever falls over. Self-hosted, `backup.sh` **is** the whole safety net —
there is no fallback behind it. Run it where it needs Docker and a look at the exit code every
night, which is the EC2 instance itself, not a laptop that might be asleep at 9pm.

```bash
sudo apt install -y git
git clone <this-repo> /opt/ans-traders   # or just copy backend/scripts/ over
```

`backup.sh` talks to `ANS_DATABASE_URL` through a throwaway Docker container either way, so pointing
it at `localhost` — the instance's own Postgres — needs nothing extra installed beyond the Docker
already on the box from section 1:

```bash
export ANS_DATABASE_URL="postgresql://ans_app:<pass>@localhost:5432/two_wheeler_spare_parts?sslmode=disable"
./scripts/backup.sh /var/backups/ans-traders
```

A dump sitting on the same disk as the database it came from protects against nothing except a bad
migration — the actual point of a backup is surviving the box, not just a bad `UPDATE`. Sync it off
the instance in the same cron job, to a bucket the EC2 role can write but not delete from (a bucket
with versioning and a short lifecycle rule is enough; the instance role only needs `s3:PutObject`):

```
0 21 * * *  cd /opt/ans-traders/backend && \
  ANS_DATABASE_URL="postgresql://ans_app:<pass>@localhost:5432/two_wheeler_spare_parts?sslmode=disable" \
  ./scripts/backup.sh /var/backups/ans-traders && \
  aws s3 sync /var/backups/ans-traders s3://<your-backup-bucket>/ans-traders/ \
  >> /var/log/ans-backup.log 2>&1
```

Verify through the same SSM tunnel used for migrations, from a laptop — this restores into the
laptop's local Docker Postgres, which is the stronger test regardless of where the dump came from,
because it proves the dump can be brought back somewhere other than the box that made it:

```bash
aws s3 cp s3://<your-backup-bucket>/ans-traders/<file>.dump ~/ANS-Traders-Backups/
./scripts/verify-backup.sh ~/ANS-Traders-Backups/<file>.dump
```

**Run this at least once**, and again after any migration. See `scripts/README.md` for what the
verify step actually checks.

---

## Deploying from GitHub Actions

Two workflows in `.github/workflows/`:

| File | When | What |
|---|---|---|
| `ci.yml` | every branch and PR | backend tests, frontend lint and type-check |
| `deploy.yml` | push to `main`, or by hand | test → **approval** → migrate → API → UI → verify |

`deploy.yml` waits on the `production` environment, so nothing reaches the shop until somebody
presses approve. Set that up once in **Settings → Environments → production → Required reviewers**;
without a reviewer the environment exists but the gate does nothing.

Order inside the deploy job is deliberate: the migration runs first, on the **direct** connection,
so the function wakes up to the schema it was built against and a failed migration stops the deploy
instead of taking a running shop down.

The database has no public IP (section 1), so **two** steps in this job need the same SSM
port-forward a laptop uses in section 2 — migrate, and the verify step at the end.
`check-registers.sh` reads the registers over HTTPS through the deployed API, which needs no
tunnel, but it mints the short-lived session token it signs in with by inserting a row straight into
Postgres first (see the comment at the top of that script), so it needs `ANS_DATABASE_URL` pointed
at the same tunnel too. Simplest: open the port-forward once near the start of the job and leave it
running for both steps, rather than opening and closing it twice. The deploy user's AWS credentials
need `ssm:StartSession` on the instance in addition to the Lambda/S3 permissions `deploy-function`
already needs.

### No S3 bucket, no ECR

At about 10MB the bundle uploads directly, so `deploy-function` needs nothing but credentials and a
region. If you ever go back to a self-contained build, read the runtime section above first — that
is the change that puts the zip over Lambda's 50MB direct-upload limit and drags an S3 bucket back
into the deploy.

### Secrets and variables

**Secrets** — Settings → Secrets and variables → Actions → Secrets:

| Name | What |
|---|---|
| `AWS_ACCESS_KEY_ID` | deploy user's key — needs `ssm:StartSession` on the DB instance too |
| `AWS_SECRET_ACCESS_KEY` | deploy user's secret |
| `DB_INSTANCE_ID` | the EC2 instance's id, to open the tunnel against |
| `DB_CONNECTION_DIRECT` | `localhost:5432` connection string, over the tunnel — migrations **and** the verify step's session token |
| `CLOUDFLARE_API_TOKEN` | token with Pages:Edit |
| `CLOUDFLARE_ACCOUNT_ID` | Cloudflare account id |

**Variables** — the same page, Variables tab. These are not credentials and are worth being able to
read at a glance:

| Name | Example |
|---|---|
| `AWS_REGION` | `ap-south-1` |
| `CF_PAGES_PROJECT` | `ans-traders` |
| `VITE_API_BASE_URL` | `https://api.your-domain.com` |
| `API_URL` | `https://api.your-domain.com` |
| `APP_URL` | `https://your-domain.com` |

### What the workflow deliberately does not do

**It never sets the function's environment variables.** `--environment-variables` replaces the whole
environment rather than merging into it, so a workflow that passed a partial set would silently drop
the connection string. `ConnectionStrings__Default`, `Cors__AllowedOrigins__0` and `Shop__TimeZone`
are set once in the console and left alone.

**It never creates the owner account.** `--create-owner` prints a password once, and a workflow log
is not where that belongs. Run it from a terminal.

**It does not roll back.** A migration that fails stops the deploy before the function is touched,
which is the case worth protecting. A bad deploy that gets through is rolled back by deploying the
previous commit — and if the migration was the problem, from the backup.

### The verify step

After the deploy, `check-registers.sh` runs against production. It is read-only: it mints a
short-lived session row, reads every register through the API, and removes it. A failure here means
the deploy went out and the books disagree with themselves — read the named check before doing
anything else.

---

## Limits worth knowing before you hit them

**A Function URL response is capped at 6MB.** Registers are deliberately not paged — half of March
is worse than none of it — so a year of stock movements or a big sales register will grow past that.
It is fine at this shop's size today. When it stops being fine, the answer is to write the export to
S3 and hand back a link, not to page the register.

**Catalogue import is one request.** Five thousand rows are validated and written together, all or
nothing. Set the function timeout high enough (60s is a reasonable start) and keep an eye on it.

**A cold Lambda takes a moment.** The database is always up now — a self-hosted EC2 instance does
not suspend after inactivity the way Neon's free tier did — but the first request against a Lambda
that has not run in a while still pays a cold start. Not broken, just the first bill of the morning.

**The EC2 instance is a single point of failure the managed option was not.** No automatic failover,
no point-in-time restore from a provider dashboard — an instance that dies loses the database until
someone launches a replacement and restores the last backup onto it. Reasonable for one shop; worth
knowing before assuming otherwise. If that ever stops being an acceptable risk, RDS is the managed
step up that keeps everything else in this file — VPC, security groups, SSM tunnel — unchanged.
