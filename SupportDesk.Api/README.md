# Support Desk

An internal tool for tracking customer support tickets from creation through resolution and
closure: an ASP.NET Core 8 Web API backed by SQL Server, and an Angular 18 single-page frontend.

The interesting part of this assignment is not the CRUD - it's the workflow. This README spends
most of its words on where the workflow rules live and why, per the assignment brief.

## Overview

Agents belong to a department (Technical, Billing, General) and can be active or inactive.
Tickets are raised against a customer, carry a priority and a status, are optionally assigned to
an agent, and carry a comment thread. The backend is authoritative for every workflow rule -
due dates, status transitions, assignment eligibility, and the closed-ticket read-only state are
all enforced server-side regardless of what the Angular client sends. The frontend mirrors those
rules for a responsive UX (e.g. hiding invalid status buttons, disabling inactive agents in
dropdowns), but a bypass attempt still gets rejected by the API with a clear error.

## Architecture

**Backend** - a single ASP.NET Core Web API project (`SupportDesk.Api`), deliberately not split
into multiple Clean-Architecture-style projects: this is a small, focused API and extra project
boundaries would add navigation overhead without adding real separation of concerns.

```
SupportDesk.Api/
  Domain/
    Entities/        Agent, Ticket, Comment, TicketReferenceCounter (EF Core entities)
    Enums/            TicketPriority, TicketStatus, Department
    Rules/            TicketDueDateCalculator, TicketStatusRules, TicketOverdueCalculator
                       - pure, static, unit-testable - the actual "business logic" of the assignment
  Data/
    SupportDeskDbContext.cs
    SqlTicketReferenceGenerator.cs   concurrency-safe TCK-YYYY-NNNN generation
    SeedData.cs
  Dtos/               request/response DTOs - EF entities are never returned from a controller
  Services/           TicketService, AgentService - own the workflow, controllers stay thin
  Controllers/        TicketsController, AgentsController - thin, no business logic
  Common/
    Exceptions/       AppException, NotFoundException, BusinessRuleException
    Middleware/       ExceptionHandlingMiddleware - centralized error -> JSON translation
  Migrations/
```

Controllers call into `ITicketService`/`IAgentService`. Every workflow rule (due dates, status
transitions, assignment eligibility, closed-read-only, overdue) lives in `Domain/Rules` as small
static classes with no dependencies, so they're trivial to unit test in isolation, and
`TicketService` composes them consistently across every mutation endpoint - the rule is defined
once and reused, not re-implemented per endpoint.

**Frontend** - `SupportDesk.Web`, an Angular 18 standalone-components app (no NgModules).

```
SupportDesk.Web/src/app/
  core/
    models/           TypeScript interfaces mirroring the API DTOs
    services/         TicketService, AgentService - the ONLY place HttpClient is used
    util/             extractErrorMessage - turns API error responses into readable text
  shared/             StatusBadge, PriorityBadge, ConfirmDialog - small reusable presentational components
  pages/
    ticket-list/      search, filters, pagination, overdue highlighting
    ticket-detail/     full ticket view, comments, assignment, status actions
    ticket-form/       create/edit with Reactive Forms
```

Components never call `HttpClient` directly; they depend on `TicketService`/`AgentService`. There
is no global state-management library - each page loads what it needs from the service layer and
holds it as plain component state, which is enough for an app this size.

## Running locally

### Prerequisites

- .NET 8 SDK
- Node.js 18+ and npm
- SQL Server LocalDB (installed with Visual Studio's "ASP.NET and web development" workload, or
  standalone via the SQL Server Express LocalDB installer)

### Backend

```bash
cd SupportDesk.Api
dotnet tool install --global dotnet-ef   # first time only
dotnet ef database update                # creates the DB and applies migrations
dotnet run
```

The API starts on `http://localhost:5089` (Swagger UI at `/swagger`) and seeds the database with
5 agents and 20 tickets automatically the first time it runs against an empty database (see
`Data/SeedData.cs` and `Program.cs`). The connection string in `appsettings.json` points at
`(localdb)\MSSQLLocalDB`; change `ConnectionStrings:SupportDesk` if you use a different SQL
Server instance.

To reset the demo data at any point:

```bash
dotnet ef database drop --force
dotnet ef database update
```

### Frontend

```bash
cd SupportDesk.Web
npm install
npm start
```

Open `http://localhost:4200`. The dev server proxies `/api/*` to `http://localhost:5089`
(`proxy.conf.json`), so the Angular app never needs a hardcoded backend URL and there's nothing
extra to configure. The API also has a CORS policy allowing `http://localhost:4200` directly, in
case you run `ng serve` without the proxy or serve a production build separately.

## Business rules

Every rule below is enforced in `SupportDesk.Api` (`Domain/Rules` + `Services/TicketService.cs`)
and independently verified by a backend test in `SupportDesk.Api.Tests`. The Angular app mirrors
the same rules only for UX (hiding buttons, disabling fields) - it is never the source of truth.

### Due date calculation

`Domain/Rules/TicketDueDateCalculator.cs`. On creation, `DueDate = CreatedDate + offset`, where
the offset is Critical → 4h, High → 1 day, Normal → 3 days, Low → 7 days.

When a ticket's priority changes (`PUT /api/tickets/{id}`), `TicketService.UpdateTicketAsync`
recalculates `DueDate` from the ticket's **original** `CreatedDate`, never from "now" - so editing
priority twice in a row always converges on the same due date for a given priority, regardless of
when the edit happened. `TicketDueDateCalculatorTests` covers this directly with the exact
worked example from the assignment brief.

### Status transitions

`Domain/Rules/TicketStatusRules.cs` defines the only valid transitions as a lookup table:

```
New         → InProgress
InProgress  → Resolved
Resolved    → Closed
Resolved    → InProgress   (reopen)
Closed      → (nothing - terminal)
```

This is exposed through a **dedicated endpoint**, `PATCH /api/tickets/{id}/status`, rather than
folded into the generic `PUT /api/tickets/{id}`. Status changes have side effects that don't apply
to a normal field edit (setting `ResolvedDate`/`ClosedDate`, checking agent assignment) and mixing
that into a general-purpose update endpoint would make both harder to reason about and easier to
get wrong. `GET /api/tickets/{id}` returns `allowedNextStatuses`, computed from the same rule
table, so the UI only ever offers actions that are currently legal.

An invalid transition returns `409 Conflict` with a machine-readable body:

```json
{ "code": "INVALID_STATUS_TRANSITION", "message": "A ticket in 'New' status cannot transition to 'Closed'." }
```

### Assignment rules

- `New → InProgress` requires a currently-assigned, currently-active agent
  (`AGENT_REQUIRED_FOR_IN_PROGRESS` / `INACTIVE_AGENT`). This is re-checked at the moment of the
  transition, not just at assignment time - an agent assigned while active but deactivated
  afterward will correctly block the transition (see
  `ChangeStatusAsync_NewToInProgress_IsRejectedWhenTheAssignedAgentHasSinceBecomeInactive`).
- An inactive agent can never be assigned to a ticket at all (`PATCH /api/tickets/{id}/assignment`
  and ticket creation both check `Agent.Active` and reject with `INACTIVE_AGENT`).
- The Angular assignment dropdowns only fetch active agents (`GET /api/agents?activeOnly=true`),
  so a user would have to bypass the UI entirely to attempt this - and the backend would still
  reject it.

### Closed ticket read-only behavior

Once a ticket is `Closed`, `TicketService.EnsureNotClosed` rejects every mutation on it: field
updates, priority changes, assignment/unassignment, further status changes, and new comments all
return `409 TICKET_CLOSED`. The Angular ticket detail page shows a persistent "read-only" banner,
hides the Edit link, hides the assignment controls, hides the comment form, and renders no status
action buttons once `ticket.status === 'Closed'`. Deletion is intentionally **not** blocked on a
closed ticket - see Assumptions below.

### Resolved → In Progress (reopening)

Reopening a resolved ticket clears `ResolvedDate` back to `null`. The reasoning: `ResolvedDate`
should always answer "when did this most recently become Resolved", and once the ticket is back
in progress that no longer has a truthful answer. If it's resolved again later, `ResolvedDate` is
set again at that moment. This keeps the field's meaning simple and consistent rather than trying
to preserve a history of every resolve/reopen cycle (which a dedicated audit log would be a better
fit for, if that were ever needed - see Improvements).

### Server-controlled fields

`Reference`, `CreatedDate`, `LastModifiedDate`, `DueDate`, `ResolvedDate`, and `ClosedDate` are
absent from every request DTO (`CreateTicketRequest`, `UpdateTicketRequest`) - there is no field
for a client to even attempt to set them through. `LastModifiedDate` is stamped by
`TicketService` on every successful mutation using an injected `TimeProvider` (not
`DateTime.UtcNow` directly), which is also what makes the due-date and overdue tests deterministic.

### Overdue

`Domain/Rules/TicketOverdueCalculator.cs`: `IsOverdue = DueDate < now && Status not in (Resolved, Closed)`.
This single function is used both when mapping a ticket to its DTO (`isOverdue` field on every
ticket response) and is mirrored as a plain SQL predicate for the `overdueOnly` list filter, so
overdue tickets can be filtered server-side without loading the whole table. The Angular app never
computes "overdue" itself - it only ever displays the `isOverdue` flag the API returns.

### Ticket reference generation

`Data/SqlTicketReferenceGenerator.cs`. References look like `TCK-2026-0001`. A `TicketReferenceCounters`
table holds one row per calendar year (`Year`, `LastNumber`). Generating the next reference runs a
single `MERGE ... WITH (HOLDLOCK)` statement inside a transaction, which takes and holds a lock on
that year's row for the statement's duration - so concurrent ticket creations for the same year are
serialized against each other by SQL Server itself and each gets a distinct, gap-free number. This
avoids both an in-memory counter (which would reset on restart and break under multiple instances)
and a naive "SELECT max, add one, save" pattern (which races under concurrency).

## API

All responses are JSON; enums are serialized as strings (`"InProgress"`, not `1`).

| Method | Route | Notes |
|---|---|---|
| GET | `/api/tickets` | Paginated, filterable list. Query params: `page`, `pageSize`, `search` (matches reference/title/customer), `status`, `priority`, `assignedAgentId`, `overdueOnly`. |
| GET | `/api/tickets/{id}` | Full ticket detail including comments and `allowedNextStatuses`. |
| POST | `/api/tickets` | Create. Body: title, description, customerName, customerEmail, priority, optional assignedAgentId. |
| PUT | `/api/tickets/{id}` | Update editable fields (title/description/customer/priority). Rejected on a Closed ticket. |
| DELETE | `/api/tickets/{id}` | Deletes the ticket and its comments (cascade). |
| PATCH | `/api/tickets/{id}/status` | Body: `{ "status": "InProgress" }`. Dedicated status-transition endpoint - see above. |
| PATCH | `/api/tickets/{id}/assignment` | Body: `{ "agentId": 3 }` or `{ "agentId": null }` to unassign. |
| POST | `/api/tickets/{id}/comments` | Body: `{ "authorName": "...", "body": "..." }`. Rejected on a Closed ticket. |
| GET | `/api/agents` | Optional `search`, `activeOnly` query params. |
| GET | `/api/agents/{id}` | Single agent. |

**Errors.** Business-rule and not-found failures return a consistent, machine-readable shape via
`ExceptionHandlingMiddleware`:

```json
{ "code": "AGENT_REQUIRED_FOR_IN_PROGRESS", "message": "A ticket must have an assigned agent before it can move to In Progress." }
```

Model-validation failures (missing/malformed fields) use ASP.NET Core's built-in
`ValidationProblemDetails` (400) shape instead, since that's already a standard, machine-readable
format and reimplementing it would add nothing. Unhandled exceptions are logged server-side and
returned to the client as a generic `500 { "code": "INTERNAL_ERROR", ... }` - no stack traces or
EF internals ever reach the response body.

## Testing

### Backend

```bash
cd SupportDesk.Api
dotnet test
```

38 tests in `SupportDesk.Api.Tests`, covering:

- `TicketDueDateCalculatorTests` - the per-priority offsets, and the priority-change-recalculates-
  from-original-CreatedDate example from the assignment brief.
- `TicketStatusRulesTests` - every valid transition, every invalid transition (including that a
  Closed ticket has no valid transitions at all), and the allowed-next-statuses lookup.
- `TicketOverdueCalculatorTests` - overdue for open tickets past due, never overdue once Resolved
  or Closed regardless of due date.
- `TicketServiceTests` (EF Core InMemory provider) - the workflow rules end-to-end through the
  actual service: valid/invalid status transitions, InProgress requiring an active assigned agent
  (including an agent that was active at assignment time and later deactivated), inactive-agent
  assignment rejection, a Closed ticket rejecting every mutation (update/assign/comment) via a
  single parameterized test, Resolved → InProgress clearing `ResolvedDate`, due-date recalculation
  from the original `CreatedDate`, and the `overdueOnly` list filter.

`SqlTicketReferenceGenerator` relies on a SQL Server-specific `MERGE ... HOLDLOCK` statement that
has no meaningful equivalent against the EF Core InMemory provider used by the other tests, so
`TicketServiceTests` exercises the service against a small in-memory `FakeTicketReferenceGenerator`
instead - the reference-generation *mechanism* is intentionally out of scope for these tests, which
focus on workflow correctness rather than storage plumbing.

### Frontend

```bash
cd SupportDesk.Web
npm test -- --watch=false --browsers=ChromeHeadless
```

11 tests across 3 spec files:

- `ticket.service.spec.ts` - verifies `TicketService` sends the right HTTP method, URL, query
  params and body for filtering, status changes, assignment, and creation (via
  `HttpClientTestingModule`).
- `ticket-list.component.spec.ts` - verifies the search input is debounced (300ms) and only
  triggers one server call with the final typed value, and that changing a filter resets to page 1.
- `ticket-detail.component.spec.ts` - verifies an open ticket shows status actions and an enabled
  comment form, and that a Closed ticket hides every mutation control (status buttons, comment
  form, assignment dropdown, edit link) behind the read-only banner.

If `ng test` can't find Chrome automatically, set the `CHROME_BIN` environment variable to your
Chrome executable path first.

## Assumptions

Where the assignment left room for interpretation:

- **Deletion of a Closed ticket is allowed.** Closed-ticket read-only rules (assignment 8) are
  about *editing* a ticket's content/workflow state, not about an administrator removing the
  record entirely. Delete is treated as a separate, administrative action and is available
  regardless of status, with a confirmation step in the UI.
- **Comment/ticket relationship on delete.** Comments are owned by their ticket (`Comment.TicketId`
  is required, cascade-delete configured in `SupportDeskDbContext`) - there's no standalone use for
  a comment without its ticket, so deleting a ticket deletes its comments with it rather than
  orphaning or soft-deleting them.
- **Resolved → InProgress clears ResolvedDate.** See "Resolved → In Progress" above.
- **Assigning an agent at ticket creation** is allowed (optional `assignedAgentId` on
  `CreateTicketRequest`), subject to the same active-agent check as the dedicated assignment
  endpoint - it seemed unnecessarily restrictive to require a two-step "create, then assign" flow
  when the assignment rule is identical either way.
- **Agent CRUD** was left at read-only (`GET` list + `GET` by id) per the assignment explicitly
  calling this optional and asking not to build endpoints without a reason.
- **SQL Server LocalDB** over PostgreSQL: this repository is being developed and reviewed on
  Windows with Visual Studio already present, where LocalDB requires no extra installation step.

## Improvements

Given more time:

- Optimistic concurrency (a `RowVersion` column) on `Ticket`, so two agents editing the same
  ticket at once get a clear conflict instead of a silent last-write-wins.
- An audit trail of status/assignment changes (who/when), rather than only the current
  `ResolvedDate`/`ClosedDate` snapshot - would also make "Resolved → InProgress" history-preserving
  without complicating the current fields.
- Authentication/authorization (the assignment doesn't call for it, so none is implemented -
  anyone who can reach the API can act as any agent).
- A "assign to me" quick action once there's a notion of a logged-in agent.
- Integration tests against a real SQL Server instance (e.g. via Testcontainers) to also exercise
  `SqlTicketReferenceGenerator`'s concurrency behavior directly, rather than only unit-testing
  workflow logic against the InMemory provider.

## Time taken

_Placeholder - update with your actual time spent._
