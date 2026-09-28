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

## Getting Started

### Backend

```powershell
dotnet build Src\Backend\TaskMaster.sln
dotnet run --project Src\Backend\TaskMaster.API
```

Swagger UI available at `/swagger/index.html`.

#### Database Configuration

The database engine is selected at startup. Only two settings are needed:

| Setting                            | Values                       | Default     |
| ---------------------------------- | ---------------------------- | ----------- |
| `Database:Provider`                | `SqlServer`, `PostgreSql`    | `SqlServer` |
| `Database:AutoMigrate`             | `true`, `false`              | `true`      |
| `ConnectionStrings:DefaultConnection` | Engine-specific connection string | —     |

`Database:Provider` lives in `TaskMaster.API/appsettings.json`; keep the connection string in
User Secrets rather than committing it:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=(localdb)\mssqllocaldb;Database=TaskMaster;Trusted_Connection=True;" --project Src/Backend/TaskMaster.API
```

PostgreSQL example:

```powershell
dotnet user-secrets set "Database:Provider" "PostgreSql" --project Src/Backend/TaskMaster.API
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=TaskMaster;Username=postgres;Password=postgres" --project Src/Backend/TaskMaster.API
```

All timestamps are stored and compared in UTC. On PostgreSQL the columns are `timestamp with
time zone`, so the same code behaves identically on both engines.

##### Automatic migrations

On startup the API applies any pending migrations for the selected engine, before it begins
serving requests and before the background services start. It is provider neutral: whichever
context `Database:Provider` selected applies its own migration set. A fresh or out-of-date
database is therefore ready without running `dotnet ef database update` by hand, and a failed
migration stops startup rather than surfacing later as request-time errors.

Set `Database:AutoMigrate` to `false` to disable this when migrations are applied out of band
(for example by a deployment step or container entrypoint). When several replicas start at
once, prefer applying migrations once and setting this to `false`, so the replicas only read the
schema.

##### Migrations

Each engine has its own migration set that must be kept in step with the other. The assembly
contains two design-time factories, so `--context` is always required:

```powershell
# SQL Server -> Migrations/SqlServer
dotnet ef migrations add <Name> --context SqlServerDbContext --output-dir Migrations/SqlServer --project Src/Backend/TaskMaster.API

# PostgreSQL -> Migrations/Npgsql
dotnet ef migrations add <Name> --context NpgsqlDbContext --output-dir Migrations/Npgsql --project Src/Backend/TaskMaster.API
```

`dotnet ef database update` applies migrations for whichever engine is configured. CI verifies
that neither context has drifted from its model.

##### Running the integration tests

The suite targets one engine per run, chosen by the `TASKMASTER_TEST_PROVIDER` environment
variable, and reads its connection string from `ConnectionStrings:SqlServerTesting` or
`ConnectionStrings:PostgreSqlTesting`. If the selected engine is not configured the tests are
skipped rather than failed.

```powershell
$env:TASKMASTER_TEST_PROVIDER = "PostgreSql"
dotnet test Src/Backend/TaskMaster.Test.IntegrationTests
```

### Frontend

```powershell
cd Src\Frontend\TaskMaster.UI
npm install
ng serve
```

API requests proxied to `https://localhost:7143` in dev mode.

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
- Unit tests (NUnit + Moq) and integration tests (NUnit + real database) run against both SQL Server and PostgreSQL
- GitHub Actions CI pipeline with a matrix over both database providers

## Future of the Project

- Containerisation - Dockerise the API and UI for easy reuse by other developers. Ship SDKs as NuGet packages.
- Authentication - Optional OAuth provider configuration for secured deployments.
- API Caching - Response caching layer to reduce database load.
- Long Polling - Replace iterative polling with long polling for job pull, using a common channel to coordinate worker waiting and job queuing across multi-server deployments.
- Real-Time Updates - SignalR integration for live dashboard updates (job status changes, worker activity).
- Demo Mode - Pre-seeded demo mode with sample data and cloud hosting for users to try the app without setup.
