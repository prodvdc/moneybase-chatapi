# Moneybase Chat API

A C#/.NET 8 Web API implementing a support chat queue with capacity rules, office hours, overflow handling, polling, and agent assignment. Data is persisted in SQL Server via Entity Framework Core.

## Project Structure
- `src/Moneybase.ChatApi/`
  - `Moneybase.ChatApi.csproj` – Web API project and package references.
  - `Program.cs` – App bootstrap, DI, hosted services, DB creation, and seeding.
  - `appsettings.json` – Configuration (office hours, queue rules, shifts, DB connection).
  - `Controllers/ChatController.cs` – HTTP endpoints for session lifecycle.
  - `Data/AppDbContext.cs` – EF Core DbContext and mappings.
  - `Data/DbInitializer.cs` – Seeds teams, shifts, and agents.
  - `Models/` – Domain models and enums.
  - `Services/` – Queueing, capacity, assignment, and polling logic.
  - `Options/ChatOptions.cs` – Typed configuration.
- `tests/Moneybase.ChatApi.Tests/`
  - `ChatApiFactory.cs` – Test server config with in‑memory DB.
  - `QueueAssignmentTests.cs` – Queue limit and assignment priority tests.

## Core Functionalities

### Session Creation and Queueing
- `POST /api/chat/sessions`
- Logic: `QueueService.EnqueueSessionAsync`
  - Checks queue length vs `capacity * 1.5`.
  - Allows overflow team during office hours when base queue is full.
  - Returns `429` for `queue_full` or `no_agents_available`.
  - Creates a `ChatSession` (status `Queued`) and event `Created`.

### Capacity Calculation
- Logic: `CapacityService`
  - Per agent capacity = `floor(10 * seniorityMultiplier)`.
  - Team capacity = sum of active agents on shift.
  - Max queue length = `floor(capacity * 1.5)`.

### Assignment (Junior‑First Round Robin)
- Logic: `AssignmentService.AssignOnceAsync`
  - FIFO sessions.
  - Assign in tiers: Junior → Mid → Senior → Team Lead.
  - Round‑robin within each tier.
  - Emits event `Assigned`.

### Polling and Inactivity
- `POST /api/chat/sessions/{id}/poll`
- Logic: `ChatController.Poll` + `PollMonitorService`
  - Poll updates `LastPolledAt`, resets `MissedPolls` and emits `Polled`.
  - Background monitor marks sessions `Inactive` after 3 missed polls.

### Close Session
- `POST /api/chat/sessions/{id}/close`
- Logic: `ChatController.Close`
  - Sets status `Closed` and emits `Closed`.

### Event History
- `GET /api/chat/sessions/{id}/events`
- Returns ordered lifecycle events with timestamps and metadata.

## Core Functions (by file)
- `Program.cs`
  - Service registration, EF Core, hosted services, seeding.
- `Controllers/ChatController.cs`
  - `CreateSession`, `Poll`, `Close`, `GetSession`, `GetEvents`.
- `Services/CapacityService.cs`
  - `IsOfficeHours`, `GetAssignableAgentsAsync`, `GetAgentCapacity`, `GetTeamCapacityAsync`, `GetMaxQueueLength`.
- `Services/QueueService.cs`
  - `EnqueueSessionAsync`.
- `Services/AssignmentService.cs`
  - `AssignOnceAsync`.
- `Services/PollMonitorService.cs`
  - `MonitorAsync`.
- `Data/DbInitializer.cs`
  - `SeedAsync` for teams, shifts, agents.

## Configuration
Edit `src/Moneybase.ChatApi/appsettings.json`:
- `ChatOptions` – office hours, max concurrency, queue multiplier, poll settings.
- `TeamSchedules` – shift windows for Team A/B/C and Overflow.
- `ConnectionStrings:ChatDb` – SQL Server connection string.

## Run
```bash
dotnet run --project src/Moneybase.ChatApi
```

## Test
```bash
dotnet test tests/Moneybase.ChatApi.Tests
```
