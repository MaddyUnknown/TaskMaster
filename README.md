# TaskMaster

Distributed task/job processing system with a .NET 8 backend, Angular 18 frontend, and producer/consumer SDKs.

## Tech Stack

- **Backend:** .NET 8 / ASP.NET Core 8, Entity Framework Core 8, SQL Server, Serilog, Swashbuckle
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

Swagger UI available at `/swagger/index.html`. Connection string stored in User Secrets under `ConnectionStrings:DefaultConnection`.

### Frontend

```powershell
cd Src\Frontend\TaskMaster.UI
npm install
ng serve
```

API requests proxied to `https://localhost:7143` in dev mode.

## What Has Been Achieved

- REST API with full CRUD for jobs, workers, and job types
- Concurrent job claiming via SQL UPDLOCK hints
- Worker heartbeat and automatic expiry
- Dashboard endpoints for system health, metrics, and activity
- Producer SDK with JSON Schema validation and in-memory caching
- Consumer SDK with handler registration pattern, pull-process loop, DI and standalone modes
- Angular SPA dashboard with job/worker browsing, detail views, charts, and job type management
- Structured logging with Serilog and correlation IDs
- Global exception handling and explicit input validation
- Unit tests (NUnit + Moq) and integration tests (NUnit + real SQL Server)
- GitHub Actions CI pipeline

## Future of the Project

- Containerisation - Dockerise the API and UI for easy reuse by other developers. Ship SDKs as NuGet packages.
- Authentication - Optional OAuth provider configuration for secured deployments.
- API Caching - Response caching layer to reduce database load.
- Long Polling - Replace iterative polling with long polling for job pull, using a common channel to coordinate worker waiting and job queuing across multi-server deployments.
- Real-Time Updates - SignalR integration for live dashboard updates (job status changes, worker activity).
- Demo Mode - Pre-seeded demo mode with sample data and cloud hosting for users to try the app without setup.
