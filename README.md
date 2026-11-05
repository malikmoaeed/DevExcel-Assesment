# Stockroom — Inventory & Stock Management

A .NET 10 MVC implementation of the DevExcel IT Solutions assessment. Products are managed separately from an append-only movement ledger. Stock is always **SUM(In) − SUM(Out)**; there is no stock balance column or stock editor.

## Quick start (Windows / PowerShell)

Install the .NET 10 SDK and SQL Server Express LocalDB. The development machine used SDK 10.0.302 and LocalDB 17.0.4025.3. Run these commands from the solution directory:

```powershell
dotnet tool restore
dotnet restore
dotnet build
sqllocaldb start MSSQLLocalDB
dotnet ef database update --project src/InventoryManagement.Infrastructure --startup-project src/InventoryManagement.Web
dotnet test
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project src/InventoryManagement.Web --no-launch-profile --urls http://localhost:5080
```

Open http://localhost:5080/Products. Stop the running application with Ctrl+C before rebuilding on Windows, where running assemblies can be locked. Visual Studio users can open `InventoryManagement.sln` and select Web as the startup project. No JavaScript build step or external CDN is required.

## Stack and architecture

C#, ASP.NET Core MVC / Razor, .NET 10, EF Core SQL Server 10.0.14, Bootstrap 5, xUnit, built-in DI and ILogger. Dependencies are pinned. The explicit cryptography dependency selects a patched package; restore vulnerability warnings are treated as errors.

```text
InventoryManagement.sln
src/
  InventoryManagement.Domain/          Entities, Enums, Exceptions, Common
  InventoryManagement.Application/     DTOs, Interfaces, Services, Exceptions
  InventoryManagement.Infrastructure/  Repositories, Persistence/Configurations,
                                       Persistence/Migrations, Persistence/Seed
  InventoryManagement.Web/             Controllers, ViewModels, Filters,
                                       Views/Products, Views/Shared, wwwroot
tests/
  InventoryManagement.Domain.Tests/
  InventoryManagement.Application.Tests/
  InventoryManagement.Infrastructure.Tests/
```

Domain references no other project. Application references only Domain. Infrastructure references Application and implements its repository contract. Web references Application and Infrastructure (the latter for the composition root). Services implement use cases; controllers bind HTTP input, call services and select views. DTOs carry read models; ViewModels carry form validation. Razor formats supplied results without querying or calculating balances. Empty architectural folders and redundant service/repository layers are intentionally omitted.

## Model and business rules

Product contains Id, Sku, Name, Description, ReorderLevel, IsActive and CreatedUtc. SKU is trimmed and uppercased invariantly; the unique SQL index also uses an explicit case-insensitive collation. Name/SKU are required, reorder level is nonnegative. Limits: SKU 64, name 200, description 2000 characters.

StockMovement contains Id, ProductId, Type (In=1/Out=2), positive Quantity, optional Note (1000 characters), CreatedUtc and a unique RequestId. Timestamps use UTC and are displayed explicitly as UTC. Foreign keys restrict deletion; SQL checks enforce positive quantity, valid type and nonnegative reorder level. Domain objects have private setters. No movement edit/delete endpoint exists; the context also rejects movement changes/deletions. Corrections are new compensating movements, subject to the same availability rule.

Balances use SQL aggregate expressions with `long` operands to avoid 32-bit aggregate overflow. An empty ledger returns zero. Low stock includes equality (`stock <= reorder level`), including inactive products. History sorts newest first, then by movement ID for deterministic ties.

## Stock workflows and concurrency

Stock In validates the entity, locks and loads the product, checks its active status, appends a receipt, commits, and returns the derived balance. Stock Out follows the same path but checks actual available stock inside the transaction. A rejection reports both requested and available quantities; no rejected row is saved.

`InventoryRepository.RecordAsync` starts a **serializable SQL Server transaction**, then selects the specific product using **UPDLOCK, HOLDLOCK** before reading any movements. Two writers for the same product cannot both hold the update lock: the second waits until the first commits/rolls back, then recalculates the balance. Both In and Out follow this protocol. An update that deactivates the product also requires a conflicting database lock, so movement validation is ordered with deactivation. Product locks can escalate under pressure, which affects throughput but not correctness.

The application supplies the active/availability policy as a callback; Infrastructure invokes it only after acquiring the lock. This keeps business rules in Application and SQL-specific transaction mechanics in Infrastructure. Transaction disposal rolls back any uncommitted work, including validation failures and cancellation. Deadlocks, lock timeouts and duplicate request conflicts become friendly retry messages; the application does not silently replay uncertain writes.

Each rendered movement form has a request UUID protected by a unique database index. Repeating the same request/payload returns the current stock without inserting another movement. Reusing that identifier with changed data is rejected. This covers double clicks and HTTP retries; a new form/new identifier is a new business request. SQL clients with unrestricted database access could bypass the service protocol; production deployment must restrict write access to the application. This is not a ledger protected against a database administrator.

See Microsoft's [SQL Server table hint documentation](https://learn.microsoft.com/en-us/sql/t-sql/queries/hints-transact-sql-table) for update/hold lock semantics.

## UI and error handling

- `/Products`: SKU/name search, computed stock, reorder level, active status, low-stock badges, details/edit links and empty states.
- `/Products/Details/{id}`: product information, balance, newest-first ledger, receipt/issue form and success/error feedback.
- `/Products/Create` and `/Products/Edit/{id}`: validated product fields and active switch; deactivation requests confirmation. Deactivation preserves inventory audit history.

Server-side DataAnnotations supplement domain validation. Duplicate SKU checks give early feedback and the database constraint closes the race. All modifying actions use anti-forgery validation and POST/redirect/GET after success. Razor encodes user content. Missing products produce a friendly 404; unexpected errors go to a generic error screen in every environment and are logged by ASP.NET Core without displaying SQL errors or stack traces.

## Database configuration and migrations

Default: `Server=(localdb)\MSSQLLocalDB;Database=DevExcelInventory;Trusted_Connection=True;TrustServerCertificate=True`.

Override with the `ConnectionStrings__Inventory` environment variable. Windows integrated authentication needs no password. LocalDB is per-user: start the application/tests as the same Windows user who owns the instance. `Database:Initialize=true` applies migrations and seeds at startup. For a deployed environment, apply migrations as a deployment step and set `Database__Initialize=false`; configure a server certificate instead of trusting it.

```powershell
# Add a future schema migration:
dotnet ef migrations add DescriptiveChangeName --project src/InventoryManagement.Infrastructure --startup-project src/InventoryManagement.Web --output-dir Persistence/Migrations
# Apply checked-in migrations:
dotnet ef database update --project src/InventoryManagement.Infrastructure --startup-project src/InventoryManagement.Web
# Verify model matches migrations:
dotnet ef migrations has-pending-model-changes --project src/InventoryManagement.Infrastructure --startup-project src/InventoryManagement.Web
```

The idempotent initializer seeds only an empty product database. A transaction-owned SQL application lock serializes concurrent seeders; existing data is never cleared.

| Seed product | In | Out | Current | Reorder | Low stock |
|---|---:|---:|---:|---:|---|
| iPhone 15 Pro | 18 | 8 | 10 | 5 | No |
| Samsung Galaxy S24 | 12 | 9 | 3 | 5 | Yes |
| Dell Latitude 5440 | 10 | 7 | 3 | 3 | Yes |
| HP EliteBook 840 | 8 | 2 | 6 | 2 | No |
| Logitech Wireless Mouse | 30 | 6 | 24 | 10 | No |

## Tests

```powershell
dotnet test
dotnet test tests/InventoryManagement.Domain.Tests
dotnet test tests/InventoryManagement.Application.Tests
dotnet test tests/InventoryManagement.Infrastructure.Tests
dotnet test --logger "trx;LogFilePrefix=inventory"
```

Domain tests cover sums, empty stock, large totals, validation, normalization and reorder boundaries. Application tests cover successful movements, insufficient stock, invalid quantities, missing/inactive products and duplicate SKUs. The test double is used only for application policy tests, not relational behavior.

Infrastructure tests run **real SQL Server LocalDB**, never EF InMemory/SQLite for SQL Server locking. Each test creates and deletes its own `InventoryTests_<guid>` database using checked-in migrations. They verify competing Out requests, idempotency, uniqueness, rollback, search, history, inactive/missing products and immutable history. The MVC test hosts the actual application against its own LocalDB and submits forms with real anti-forgery tokens. LocalDB is required for the full suite; failures are not silently skipped. Test databases need create/drop permissions and are cleaned up on normal completion. A terminated process may leave its uniquely named test database behind.

## Assumptions, limitations and future improvements

- Single organization and warehouse; integral units. No authentication/authorization was requested. This is a local hiring-assessment module, not an internet-ready multi-user security deployment.
- Users may reactivate products. Creation always starts active. No hard product deletion or movement mutation UI exists.
- Product metadata uses last-write-wins; stock transactions have explicit pessimistic protection. Add row-version conflict handling for metadata if required.
- Lists/history are unpaginated, appropriate for this assessment's data size. Add paging and load tests for large ledgers; keep aggregate correctness.
- Search follows database collation. SKU uniqueness is explicitly case-insensitive; notes/descriptions are plain text.
- Optional structured logging is included. CSV export, dashboards, JSON API, pagination and optimistic metadata concurrency are deferred.
- Production improvements: identity/roles, audit actor, least-privilege database access, deployment migrations, telemetry, accessibility audit, backups and recovery tests.

See `VERIFICATION.md` for observed results and `AI_NOTES.md` for transparent AI usage. The local Git history uses the explicit Codex identity because no author identity was configured; nothing is pushed remotely.
