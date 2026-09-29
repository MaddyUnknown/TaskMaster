# TaskMaster External Demo

A thin, external demonstration of the existing **TaskMaster** distributed job
processing platform. Two small .NET 8 applications — a public web app and a
private worker console app — show how a completely separate codebase can
consume the TaskMaster Producer and Consumer SDKs without owning any job
infrastructure.

> **TaskMaster is the product being demonstrated. The demo is only an external
> integration example.** It reimplements none of TaskMaster: job persistence,
> the job state machine, workers and reporting all stay in the platform. The
> demo owns exactly one table of its own (§6) and one directory of generated
> files (§7), both of which are just the application's own data — a report
> request and the report it produced. Neither is ever confused with job state.

---

## Contents

1. [What the demo is](#1-what-the-demo-is)
2. [Existing TaskMaster components consumed](#2-existing-taskmaster-components-consumed)
3. [Architecture](#3-architecture)
4. [Producer SDK flow](#4-producer-sdk-flow)
5. [Consumer SDK flow](#5-consumer-sdk-flow)
6. [Database: one shared table](#6-database-one-shared-table)
7. [Report storage](#7-report-storage)
8. [Result flow](#8-result-flow)
9. [Report generation](#9-report-generation)
10. [Authentication](#10-authentication)
11. [Local setup](#11-local-setup)
12. [Configuration](#12-configuration)
13. [Running the web demo](#13-running-the-web-demo)
14. [Running the consumer](#14-running-the-consumer)
15. [Connecting to an existing TaskMaster deployment](#15-connecting-to-an-existing-taskmaster-deployment)
16. [Security considerations](#16-security-considerations)
17. [Known platform gaps](#17-known-platform-gaps)

---

## 1. What the demo is

A user visits the demo web site, submits a *synthetic sales report* job, watches
it move through the real TaskMaster job lifecycle
(`queued → in-progress → completed/failed`) and finally views or downloads the
generated CSV report in the browser.

The workload is deliberately trivial (synthetic rows + simple aggregation) so
the focus stays on the platform integration, not on reporting.

The demo is a **one-shot tool**: a report is generated, viewed, downloaded, and
then discarded. Nothing accumulates — there is no account, no report list and no
history (§8).

## 2. Existing TaskMaster components consumed

| Component | Used for |
| --- | --- |
| `TaskMaster.API` | Job persistence, job lifecycle, workers, permissions |
| `TaskMaster.Library.Producer` | Job creation (`IProducer.ProduceAsync`, schema validation) |
| `TaskMaster.Library.Consumer` | Worker lifecycle (`IWorkerFactory`, `IJobHandler<T>`, polling, heartbeats, batched status reporting) |

The demo does **not** reimplement any TaskMaster functionality, and never reads
or writes the platform's own database.

## 3. Architecture

```
                            Internet
                                │  HTTPS (reverse proxy)
                                ▼
                      ┌──────────────────────┐
                      │  TaskMaster.Demo.Web │  UI + BFF endpoints
                      └──────┬──────┬────────┘
                             │      │
           Producer SDK      │      │  reads row + streams file
           (client creds)    │      │
                             ▼      │
                      ┌──────────────────────┐
                      │   TaskMaster.API     │  job persistence + lifecycle
                      └──────────┬───────────┘
                                 │ pull / report / heartbeat
                                 ▼   Consumer SDK (client creds)
                      ┌──────────────────────┐
                      │ TaskMaster.Demo.     │
                      │   Consumer           │  no inbound ports
                      └──────────────────────┘

   Demo.Web and Demo.Consumer also share exactly two things:
     · one database table  ReportRequests          (§6)
     · one file store      Storage:RootPath        (§7)
```

* Only `TaskMaster.Demo.Web` is publicly reachable.
* `TaskMaster.Demo.Consumer` has no listening sockets. It needs outbound access
  to the TaskMaster API, **and to the shared database and file store** — not to
  the web app.
* There is **no HTTP communication between the two demo processes.** Progress
  reaches the UI because both sides read and write the same row.
* The TaskMaster API and its database are never exposed publicly by the demo.

Projects:

```text
Src/Backend/Samples/
├── TaskMaster.Demo.Web         # ASP.NET Core web app (UI + BFF endpoints)
└── TaskMaster.Demo.Consumer    # Console worker (Consumer SDK handler)
```

The two projects share **no code project on purpose.** The entity, `DbContext`
and file store exist as two copies (§6, §7) — see
[Docs/TaskMaster-Demo-Agent.md](../../../Docs/TaskMaster-Demo-Agent.md) for the
reasoning.

## 4. Producer SDK flow

`TaskMaster.Demo.Web` registers the Producer SDK exactly as an external app
would:

```csharp
builder.Services.AddTaskMasterProducer(options =>
{
    options.ApiBaseUrl = configuration["TaskMaster:ApiBaseUrl"];
    options.Auth.Oidc = new TaskMasterProducerOidcAuthOptions
    {
        ClientId = ..., ClientSecret = ...
    };
});
```

Submission (`POST /api/demo/reports`) does two things, in this order:

```csharp
// 1. write the demo's own row first, so the consumer always has a target
await _reports.AddAsync(new ReportRequestEntity
{
    Id = Guid.NewGuid(),          // == ReportJobPayload.SubmissionId
    Title = request.Title,
    RecordCount = request.RecordCount,
    Status = ReportProcessingStatus.Queued,
    SubmittedAtUtc = now, ModifiedAtUtc = now
}, cancellationToken);

// 2. hand the job to TaskMaster
await producer.ProduceAsync(new ReportJobPayload
{
    SubmissionId = entity.Id,      // local correlation id
    Title = entity.Title,
    RecordCount = entity.RecordCount
});
```

The SDK fetches the JSON Schema registered for job type `demo-report` v1 from
the TaskMaster API, validates the payload, and creates the job via
`POST api/jobs` using client-credentials authentication (scopes:
`jobs:create jobtypes:read`).

Writing the row *before* producing is deliberate: the consumer may pick the job
up immediately, and it must find its row. If `ProduceAsync` then throws, the row
is marked `Failed` with the reason instead of being left dangling in `Queued`,
and the browser gets a `502`.

**No job-id discovery is needed.** `ProduceAsync` still does not return the
created job id, but the demo no longer wants it: progress is read from the demo's
own row, not from the TaskMaster jobs API (§17).

## 5. Consumer SDK flow

`TaskMaster.Demo.Consumer` registers the Consumer SDK and one application
handler:

```csharp
services.AddTaskMasterConsumer(options => { /* ApiBaseUrl + Oidc creds */ });
...
var worker = factory.CreateWorker(workerName, configuration =>
{
    configuration.Handle<ReportJobPayload, GenerateReportHandler>();
});
await worker.RunAsync(cancellationToken);
```

Everything else — worker registration with capabilities, job polling through a
bounded channel pipeline, concurrent handler execution, heartbeats, batched
completion/failure reporting with retries — is provided by the Consumer SDK
(scopes: `jobs:pull jobs:report workers:register workers:heartbeat
workers:remove jobtypes:read`).

The handler itself:

1. validates the payload defensively,
2. loads its row by `SubmissionId` (fails the job if the row is missing),
3. marks the row `Running`,
4. generates deterministic synthetic sales rows (time-budgeted),
5. formats a size-capped CSV and **writes it to the shared file store**,
6. records the stored file's metadata on the row and marks it `Completed`,
7. returns → the SDK reports the job `completed`.

On any failure it records the reason on the row and **rethrows**, so TaskMaster
reports the job `failed` too. The file is written *before* the row is marked
`Completed`, so a `Completed` row always points at a file that exists.

Note: the current TaskMaster platform tracks only the four job statuses; there
is no percentage/attempts progress field. The demo maps the real statuses onto
progress stages (`Queued → Running → Completed/Failed`) instead of inventing a
second state machine.

## 6. Database: one shared table

The demo owns a single table, `ReportRequests` — one row per report request,
holding the status the UI polls and the metadata of the file that was produced.

Supported engines (choose with `Database:Provider`):

| `Database:Provider` | Package |
| --- | --- |
| `SqlServer` | `Microsoft.EntityFrameworkCore.SqlServer` |
| `PostgreSql` | `Npgsql.EntityFrameworkCore.PostgreSQL` |

Both engines run the **same** `DemoDbContext` and the same repository code, and
all queries are plain EF LINQ — no raw SQL and no provider-specific operators.

### Why two copies of the model

`ReportRequestEntity`, `DemoDbContext` and `IReportFileStore` exist once in
`TaskMaster.Demo.Web` and once in `TaskMaster.Demo.Consumer`. They are separate
CLR types that are never loaded into the same process, so the duplication is
inert. This is deliberate: the demo must demonstrate what an *external, separate
codebase* looks like, and a shared `TaskMaster.Demo.Shared` project would blur
exactly that. The copy that **must** stay identical is the schema — see
[Docs/TaskMaster-Demo-Agent.md](../../../Docs/TaskMaster-Demo-Agent.md) for how
drift is prevented without a shared project.

### Who creates the schema

**Only `TaskMaster.Demo.Web` runs `EnsureCreated`** (controlled by
`Database:AutoMigrate`, default `true`). `EnsureCreated` is not safe to run from
two processes concurrently, and the web tier is the demo's single entry point.

* Start `TaskMaster.Demo.Web` once before the consumer and the table exists.
* Set `Database:AutoMigrate=false` when several web replicas run and the schema
  is created out of band.
* If the consumer starts first it reports a raw EF "invalid object name" /
  "does not exist" error. That is expected — the message tells you the web app
  has not created the schema yet.

There are no migrations: the demo creates one table, and the platform's own
schema is not involved.

### Who writes which column

Ownership is split so the two processes never write the same column — which is
why there is no concurrency token and no optimistic-locking retry loop:

| Column | Written by |
| --- | --- |
| `Status` (→ `Running`, `Completed`, `Failed`), file metadata | `TaskMaster.Demo.Consumer` |
| `Status` (→ `Queued`, `Failed` on submit error), row deletion | `TaskMaster.Demo.Web` |
| `Status` (→ `Failed` for abandoned rows) | `TaskMaster.Demo.Consumer` (sweeper) |

`Status` is mapped `HasConversion<int>()` so the column type never depends on
how a provider would otherwise map an enum, and `SubmittedAtUtc` is indexed for
the retention sweep.

### Abandoned jobs

A handler killed mid-flight never reaches its `MarkFailedAsync`, so its row
stays `Running` forever. `StaleReportReconciler` (in the consumer) fails any row
still `Running` with no update for longer than `Demo:StaleReportTimeoutMinutes`
(10 min by default). That value is validated at startup to exceed
`Demo:MaxExecutionSeconds`, so a healthy long job is never failed underneath
itself.

## 7. Report storage

Generated CSVs go through `IReportFileStore`, not through the database:

```csharp
public interface IReportFileStore
{
    Task<StoredReport> SaveAsync(Guid reportId, string fileName, string content, CancellationToken ct);
    Task<string?> ReadAsync(string storageKey, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken ct);
    Task<bool> DeleteAsync(string storageKey, CancellationToken ct);
}
```

Only the **storage-agnostic** `StorageKey` is ever persisted, so moving to an
object store changes the implementation and one config value — not the schema,
not the handler and not the API. `Storage:Provider` is `Local` today; `S3` and
`AzureBlob` are named in the config and fail fast with a clear message if
selected, rather than silently falling back to local disk.

The local implementation:

* lays keys out as `{yyyy}/{MM}/{reportId}.csv` — equally valid as an S3 or
  Azure Blob key,
* writes atomically (temp file + `File.Move`), so the web tier can never serve a
  half-written report,
* rejects any key that resolves outside the storage root.

> **Both apps must point at the same root.** `Storage:RootPath` is relative by
> default (`./storage/reports`) and relative paths resolve against each
> process's working directory — so run both from the same directory, or set an
> **absolute** path. This is the single most common cause of "the worker says
> Completed but the download 404s".

## 8. Result flow

The current TaskMaster API persists job payloads (input) and statuses but has
**no result/output storage**. Rather than modifying the platform, the two demo
processes share the smallest possible thing: a row and a file.

```
browser ──POST /api/demo/reports──► Demo.Web
                                      │ 1. insert row (Queued)
                                      │ 2. ProduceAsync (Task job)
                                      ▼
                                 TaskMaster.API
                                      │ job queued
                                      ▼
                                 Demo.Consumer
                                      │ 3. row → Running
                                      │ 4. generate CSV
                                      │ 5. SaveAsync  ──► shared file store
                                      │ 6. row → Completed (+ StorageKey)
                                      ▼
browser ──GET /api/demo/reports/{id}──► Demo.Web
                                      │ 7. read the row (polls until terminal)
                                      ▼
browser ──GET .../download───────────► Demo.Web streams the file
```

* The browser polls the **demo row**, not TaskMaster. A consumer restart can
  therefore never leave the UI polling a dead endpoint.
* The authoritative job lifecycle still flows entirely through TaskMaster
  (`queued/in-progress/completed/failed` via the Consumer SDK's bulk status
  reporting). The demo row is a mirror for the UI, not a second source of truth
  about the job.
* Nothing about the result travels over HTTP between the demo processes — there
  is no callback endpoint, no shared API key and no inbound port on the worker.

### Retention

Reports are disposable. `ReportRetentionService` (in the web app) sweeps every
`Demo:RetentionSweepMinutes` and, for rows submitted more than
`Demo:ReportRetentionHours` ago (24 h by default), deletes the **file first, then
the row**. Deleting in that order means a live row never points at a missing
file. After that, both the status poll and the download return `404`, and the UI
says the report expired.

Because the UI keeps its list in memory only, refreshing the page simply starts
over — that is the intended one-shot behaviour, not a bug.

## 9. Report generation

Fully synthetic and side-effect free:

```
seeded Random(submissionId)
        │
        ▼
N sales rows (timestamp, region, product, units, unit price, revenue)
        │
        ▼
per-region aggregation (orders, units, revenue)
        │
        ▼
CSV (+ '#'-prefixed summary comments), UTF-8, byte-capped
```

Limits (all configurable):

| Limit | Default |
| --- | --- |
| Records per job | 10 – 10 000 (web enforces, consumer re-validates) |
| Result size | 1 MiB (older content truncated, marker added) |
| Stored file size | 8 MiB (`Storage:MaxFileBytes`; larger content is rejected) |
| Execution budget | 120 s (`TimeoutException` ⇒ job fails) |
| Submissions | 5 per IP per minute (fixed window rate limiter) |

No email, SMS, webhooks, arbitrary HTTP, file access, SQL or shell execution is
possible from the job definition — the handler only computes synthetic numbers.

## 10. Authentication

The demo **web tier is intentionally unauthenticated** — there is no OIDC
login, no session cookie and no antiforgery token. It is a sample you run
locally, and keeping a browser auth flow would have meant carrying an identity
provider into the demo for no demonstrative value.

* The web tier is public only if you publish it. Put it behind a reverse proxy
  with TLS if you do, or keep it on loopback.
* Submissions are rate limited **per remote IP** (5/minute) — there is no user
  identity to partition on.
* Machine-to-machine access (Producer/Consumer SDKs) is separate and does
  authenticate: client-credentials clients with only the scopes listed in §2
  and §5. Those credentials come from configuration/secrets, never from the
  browser.

## 11. Local setup

Prerequisites: .NET 8 SDK, a running TaskMaster API (see root README) with its
own database, and — for the demo's own storage — a reachable SQL Server **or**
PostgreSQL instance. The two demo apps may use a different database from
TaskMaster's own; it only holds the demo's report rows.

Register the demo job type once (any client with `jobtypes:create`; e.g. via
Swagger on the API):

```bash
curl -X POST https://localhost:7143/api/job-types \
  -H "Content-Type: application/json" \
  -d '{
        "name": "demo-report",
        "version": 1,
        "description": "Demo synthetic sales report",
        "schema": <contents of Samples/TaskMaster.Demo.Web/Domain/ReportJobPayload.cs SchemaJson>
      }'
```

(The exact JSON Schema string is embedded in both demo projects next to the
payload contract. Producer and Consumer each carry their own copy of the payload
type, because each needs a different sealed `JobTypeAttribute`.)

Then point both demo apps at a database. `ConnectionStrings:DefaultConnection`
is intentionally **empty** in the committed `appsettings.json`; the apps fail
fast at startup until you set it, rather than silently using a default database.

## 12. Configuration

Secrets are never committed. Use environment variables (`__` separator),
user-secrets or your host's secret store.

**Shared by both apps** (they must agree):

| Key | Meaning |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Demo database connection string (required) |
| `Database:Provider` | `SqlServer` or `PostgreSql` |
| `Storage:Provider` | `Local` (only implemented value) |
| `Storage:RootPath` | Shared root directory — **use an absolute path** if the processes run from different working directories |
| `Storage:MaxFileBytes` | Hard ceiling on a single stored file |
| `TaskMaster:ApiBaseUrl` | Base URL of the running TaskMaster API |
| `TaskMaster:Oidc:ClientId` / `ClientSecret` | Machine client for the SDKs (empty when the API runs in `Auth:Mode=None`) |

**TaskMaster.Demo.Web/appsettings.json**:

| Key | Meaning |
| --- | --- |
| `Database:AutoMigrate` | Create the schema with `EnsureCreated` on startup (default `true`; **this app only**) |
| `Demo:DefaultRecordCount`, `MinRecordCount`, `MaxRecordCount`, `MaxTitleLength` | Report size limits |
| `Demo:MaxJobSubmissionsPerUserPerMinute` | Rate limit per IP |
| `Demo:ResultMaxBytes` | Cap on the in-browser CSV preview (the download is not capped) |
| `Demo:ReportRetentionHours`, `RetentionSweepMinutes` | Retention window and sweep interval |

**TaskMaster.Demo.Consumer/appsettings.json**:

| Key | Meaning |
| --- | --- |
| `Demo:MaxRecords`, `MaxResultBytes`, `MaxExecutionSeconds`, `RowDelayMilliseconds` | Workload guards |
| `Demo:MaxConcurrentHandlers`, `WorkerName` | Concurrency and worker identity |
| `Demo:StaleReportTimeoutMinutes`, `StaleSweepMinutes` | Abandoned-row detection |

Note there is no `Demo:WebBaseUrl` and no `Demo:InternalApiKey` any more: the
worker never calls the web app.

## 13. Running the web demo

```bash
cd Src/Backend/Samples/TaskMaster.Demo.Web

# SQL Server
set ConnectionStrings__DefaultConnection=Server=localhost;Database=TaskMasterDemo;Trusted_Connection=True;TrustServerCertificate=True

# or PostgreSQL
set Database__Provider=PostgreSql
set ConnectionStrings__DefaultConnection=Host=localhost;Database=taskmaster_demo;Username=postgres;Password=...

# absolute path, so the consumer finds the same files regardless of its CWD
set Storage__RootPath=E:\demo\reports

dotnet run
```

This creates the `ReportRequests` table on first start. Open the served URL,
choose a record count and press **Generate Report**.

## 14. Running the consumer

In a second terminal, with the **same** connection string, provider and storage
root:

```bash
cd Src/Backend/Samples/TaskMaster.Demo.Consumer
set ConnectionStrings__DefaultConnection=<same string as the web app>
set Database__Provider=SqlServer
set Storage__RootPath=E:\demo\reports
set TaskMaster__Oidc__ClientId=...        # if the API runs in OIDC mode
set TaskMaster__Oidc__ClientSecret=...
dotnet run
```

The worker registers, pulls `demo-report` jobs, prints generation logs, writes
each report to the shared store and marks the row completed — no callbacks to
the web tier. Ctrl+C shuts down cleanly (the SDK removes the worker
registration).

## 15. Connecting to an existing TaskMaster deployment

The demo assumes an already-running TaskMaster stack; point both apps at it via
`TaskMaster:ApiBaseUrl` and create the machine clients described above. Nothing
about the demo touches the platform's database or messaging internals — it only
speaks the public HTTP API through the official SDKs.

Recommended topology:

```
nginx/Caddy (TLS) ──► TaskMaster.Demo.Web (Kestrel, localhost only)
        │                        │
        │                        ├──► TaskMaster.API (private interface)
        │                        ├──► demo database + shared file store
        │                        └──► TaskMaster.Demo.Consumer (no inbound ports)
```

* Publish the web app (`dotnet publish -c Release`) and run it behind the proxy;
  terminate TLS there and keep Kestrel bound to loopback.
* Run the consumer under systemd/supervisor with no listening sockets.
* The web app and the consumer need database and storage access; keep that on a
  private network, and give the web app read/write to the store (it serves
  downloads and deletes expired files).
* Do not expose the API port or the TaskMaster database publicly.

## 16. Security considerations

* The web tier is unauthenticated by design (§10) — bind it to loopback or put
  it behind an authenticating proxy before exposing it.
* Machine-to-machine access uses least-privilege client-credentials clients;
  secrets come from configuration/user-secrets, never from the browser.
* Per-IP fixed-window rate limiting on submissions.
* Input validation at every layer; hard caps on records, runtime, result size and
  stored file size.
* No inbound port on the worker and no result-ingestion endpoint, so there is no
  internal API key to leak or misconfigure.
* No tokens/secrets logged, none committed (placeholder configs only).
* CORS disabled by default (same-origin UI). Storage keys are validated against
  the storage root, so a crafted key cannot escape it.
* The demo database holds only report rows; the demo file store holds only
  generated CSVs. Neither contains credentials or job payloads.

## 17. Known platform gaps

| Gap in current TaskMaster | Status in the demo |
| --- | --- |
| `ProduceAsync` does not return the created job id | No longer needed — the UI reads the demo's own row, not the jobs API |
| No result/output storage on jobs | Report output is the demo application's own data: one file in the shared store, addressed by the row's `StorageKey` |
| No percent/attempts progress fields | Status-based stage mapping (`Queued → Running → Completed/Failed`) |
