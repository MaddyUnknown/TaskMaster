# TaskMaster

Distributed task/job processing system with a .NET 8 backend, Angular 18 frontend, and producer/consumer SDKs.

## Tech Stack

- **Backend:** .NET 8 / ASP.NET Core 8, Entity Framework Core 8, SQL Server or PostgreSQL, Serilog, Swashbuckle
- **Frontend:** Angular 18, Lucide icons, plain CSS
- **Testing:** NUnit 4 + Moq 4 (unit), NUnit (integration)
- **CI:** GitHub Actions

## Project Structure

```
Src/
  Backend/
    TaskMaster.API/                   - Web API
    TaskMaster.Library.Common/        - Shared models, interfaces, utilities
    TaskMaster.Library.Producer/      - SDK for creating/submitting jobs
    TaskMaster.Library.Consumer/      - SDK for building workers
    TaskMaster.Test.UnitTests/
    TaskMaster.Test.IntegrationTests/
    Samples/
      TaskMaster.Sample.Producer/     - Minimal Producer SDK sample
      TaskMaster.Sample.Consumer/     - Minimal Consumer SDK sample
      TaskMaster.Demo.Web/            - External demo web app (uses the Producer SDK)
      TaskMaster.Demo.Consumer/       - External demo worker (uses the Consumer SDK)
  Frontend/
    TaskMaster.UI/                    - Angular SPA dashboard
```

## Purpose

### Backend API

Central REST API for managing jobs, workers, job types, and system monitoring. Handles job submission, queuing, concurrent claiming, completion, and failure reporting. Exposes dashboard endpoints for activity, metrics, and health checks.

### Frontend

Angular SPA dashboard for monitoring and managing the task system. Features include job and worker browsing, detail views, job type management, system health overview, charts, and activity feeds.

### Library.Producer

Client SDK for creating and submitting jobs to the API. Validates payloads against JSON Schema before sending.

### Library.Consumer

Worker SDK for building worker agents that pull queued jobs, dispatch them to registered handlers, and report completion or failure.

### Samples & Demo

`Src/Backend/Samples` holds two minimal SDK samples (`TaskMaster.Sample.Producer`, `TaskMaster.Sample.Consumer`) and a complete external demo (`TaskMaster.Demo.Web` + `TaskMaster.Demo.Consumer`) in which a separate codebase submits a synthetic report job and processes it end to end. See [`Src/Backend/Samples/README.md`](Src/Backend/Samples/README.md).

## Getting Started

Grab the latest [release](../../releases). The .NET apps are framework-dependent, so you need the
**.NET 8 runtime** on the host, not the SDK. The dashboard is prebuilt; no Node or Angular tooling
is required.

| Release asset | What it is |
| --- | --- |
| `TaskMaster.Coordinator.API.<version>.zip` | The API |
| `TaskMaster.UI.<version>.zip` | Static dashboard files |
| `TaskMaster.Library.Producer/Consumer/Common.<version>.nupkg` | SDK packages for your own apps |
| `TaskMaster.Demo.Web/Consumer.<version>.zip` | External demo (see [`Src/Backend/Samples/README.md`](Src/Backend/Samples/README.md)) |

### Run the API

Unzip, set the connection string in `appsettings.json`, then start it:

```powershell
dotnet TaskMaster.API.dll
```

Migrations run on startup, so a fresh database is ready immediately. Swagger is at
`/swagger/index.html` in Development.

Set the listen address with `ASPNETCORE_URLS`, the `Urls` key, or a Kestrel endpoint in
`appsettings.json`:

```json
{
  "Kestrel": {
    "Endpoints": {
      "Http": { "Url": "https://localhost:7143" }
    }
  }
}
```

### Configuration

Required:

| Key | Notes |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | Your database connection string |
| `Database:Provider` | `SqlServer` (default) or `PostgreSql` |

Optional:

| Key | Default | Notes |
| --- | --- | --- |
| `Database:AutoMigrate` | `true` | Set `false` when migrations are applied out of band |
| `Auth:Mode` | `None` | `Oidc` to require bearer tokens |
| `Auth:Oidc:Authority` | none | Required when `Auth:Mode=Oidc` |
| `Auth:Oidc:Audience` | none | Optional audience check |
| `Auth:Oidc:ScopeClaim` | `scope` | Ships blank in `appsettings.json`; set it or authorization fails |
| `WorkerConfig:HeartBeatIntervalSeconds` | `10` | Returned to workers at registration |
| `WorkerConfig:WorkerExpiryIntervalSeconds` | `30` | Workers silent this long are expired |

Secrets belong in environment variables (`__` separator) or user-secrets. Logging is configured
under `Serilog` (console plus a daily rolling file). Timestamps are stored and compared in UTC on
both providers.

### Authentication

`Auth:Mode=None` (the default) treats every request as a user holding all permissions. Use it for
local development, or in a closed environment where the API is not reachable publicly.

`Auth:Mode=Oidc` requires a JWT bearer token and maps OIDC scopes to TaskMaster permissions:

| Scope | Permission |
| --- | --- |
| `jobs:create` | `job.create` |
| `jobs:read`, `jobs:stats` | `job.read`, `job.stats` |
| `jobs:pull`, `jobs:report` | `job.pull`, `job.report` |
| `workers:read`, `workers:stats` | `worker.read`, `worker.stats` |
| `workers:register`, `workers:heartbeat`, `workers:remove` | matching `worker.*` permissions |
| `jobtypes:read`, `jobtypes:create` | `jobtype.read`, `jobtype.create` |
| `dashboard:read` | `dashboard.read` |

`GET /api/auth/config` is anonymous and reports the active mode.

### Dashboard

Serve the files from `TaskMaster.UI.<version>.zip` with any static host. `app-config.json` sets
`apiBaseUrl` (`/api` by default) and the OIDC `clientId` and redirect URIs for browser login.

## Producer & Consumer SDKs

Reference the `TaskMaster.Library.Producer` and `TaskMaster.Library.Consumer` packages (both .NET
8). They attach to each GitHub release; add them as a local NuGet source or reference the `.nupkg`
directly. They talk to the API over HTTP and need no database access.

Register the job type once. It is a name, a version and a JSON Schema, and the Producer validates
every payload against the schema before sending (needs `jobtypes:create`):

```bash
curl -X POST https://localhost:7143/api/job-types \
  -H "Content-Type: application/json" \
  -d '{"name":"sample-message","version":1,"schema":"{\"type\":\"object\",\"properties\":{\"Message\":{\"type\":\"string\"}},\"required\":[\"Message\"]}"}'
```

You can also register it from the dashboard's Job Types page.

Producer:

```csharp
[JobType("sample-message", 1)]   // TaskMaster.Library.Producer.Attributes
public sealed class SamplePayload { public string Message { get; set; } = ""; }

services.AddTaskMasterProducer(o =>
{
    o.ApiBaseUrl = "https://localhost:7143";
    // omit Oidc when Auth:Mode=None
    o.Auth.Oidc = new TaskMasterProducerOidcAuthOptions { ClientId = "...", ClientSecret = "..." };
});

await producer.ProduceAsync(new SamplePayload { Message = "hello" });
```

Consumer:

```csharp
[JobType("sample-message", 1)]   // TaskMaster.Library.Consumer.Attributes
public sealed record SamplePayload(string Message);

public class SampleHandler : IJobHandler<SamplePayload>
{
    public Task HandleAsync(SamplePayload payload) => Task.CompletedTask;
}

services.AddTaskMasterConsumer(o => { o.ApiBaseUrl = "https://localhost:7143"; });

var worker = factory.CreateWorker("my-worker", c => c.Handle<SamplePayload, SampleHandler>());
await worker.RunAsync(cancellationToken);
```

The Consumer SDK handles worker registration, job polling, heartbeats and batched status reporting.
Optional tuning: `MaxConcurrentHandlers` (5), `PrefetchJobPerHandler` (1), `MaxResultRetries` (5),
`PollingWaitIntervalMs` (500).

Scopes: producer needs `jobs:create jobtypes:read`; consumer needs `jobs:pull jobs:report
workers:register workers:heartbeat workers:remove jobtypes:read`.

Working examples live in `Src/Backend/Samples/TaskMaster.Sample.Producer` and
`TaskMaster.Sample.Consumer` (they read `TASKMASTER_CLIENT_ID` / `TASKMASTER_CLIENT_SECRET`). For
a complete Producer + Consumer + shared-storage walkthrough, see
[`Src/Backend/Samples/README.md`](Src/Backend/Samples/README.md).

## Building from Source

For contributors. Requires the .NET 8 SDK, Node.js 22 and a database.

```powershell
dotnet build Src\Backend\TaskMaster.sln
dotnet run --project Src\Backend\TaskMaster.API --launch-profile https
```

`--launch-profile https` binds `https://localhost:7143`; the default `http` profile binds
`http://localhost:5238`. For the dashboard, run `npm ci` then `npm run start` in
`Src\Frontend\TaskMaster.UI` (dev requests proxy to `https://localhost:7143`).

### Migrations

Each engine keeps its own migration set, so `--context` is always required:

```powershell
dotnet ef migrations add <Name> --context SqlServerDbContext --output-dir Migrations/SqlServer --project Src/Backend/TaskMaster.API
dotnet ef migrations add <Name> --context NpgsqlDbContext --output-dir Migrations/Npgsql --project Src/Backend/TaskMaster.API
```

CI verifies that neither context has drifted from its model.

### Integration Tests

The suite targets one engine per run, chosen by `Database:Provider` (override with
`Database__Provider`), and reads `ConnectionStrings:SqlServerTesting` or
`ConnectionStrings:PostgreSqlTesting`. Without a connection string the tests skip rather than fail.

```powershell
$env:Database__Provider = "PostgreSql"
dotnet test Src/Backend/TaskMaster.Test.IntegrationTests
```

## What Has Been Achieved

- REST API with full CRUD for jobs, workers, and job types
- Developer-selectable SQL Server or PostgreSQL persistence, with dialect-specific query stores behind shared interfaces
- Concurrent job claiming via SQL Server `UPDLOCK` / `READPAST` and PostgreSQL `FOR UPDATE ... SKIP LOCKED`
- Worker heartbeat and automatic expiry
- Dashboard endpoints for system health, metrics, and activity
- Producer SDK with JSON Schema validation and in-memory caching
- Consumer SDK with handler registration pattern, pull-process loop, DI and standalone modes
- Angular SPA dashboard with job/worker browsing, detail views, charts, and job type management
- Structured logging with Serilog and correlation IDs
- Global exception handling and explicit input validation
- Pluggable authentication (`Auth:Mode` = `None` | `Oidc`) with scope-based authorization, client-credentials SDK auth, and an anonymous `/api/auth/config` endpoint
- Unit tests (NUnit + Moq) and integration tests (NUnit + real database) run against both SQL Server and PostgreSQL
- GitHub Actions CI pipeline with a matrix over both database providers

## Future of the Project

- Containerisation - Dockerise the API and UI for easy reuse by other developers. Ship SDKs as NuGet packages.
- API Caching - Response caching layer to reduce database load.
- Long Polling - Replace iterative polling with long polling for job pull, using a common channel to coordinate worker waiting and job queuing across multi-server deployments.
- Real-Time Updates - SignalR integration for live dashboard updates (job status changes, worker activity).
- Demo Mode - Pre-seeded demo mode with sample data and cloud hosting for users to try the app without setup.
