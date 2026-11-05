# Assessment implementation plan

All four pages of the supplied assessment were read. Required: .NET 10, EF Core, server-rendered product list/detail/edit screens, search, unique SKU, derived stock, positive quantities, friendly validation, concurrent oversell prevention, tests, incremental Git history, README and honest AI notes. Optional PDF features are deferred until the core is verified.

The user's narrower choices are MVC, Clean Architecture, SQL Server LocalDB, Bootstrap 5, migrations and xUnit. Domain and Application remain independent of EF and HTTP. A serializable transaction acquires an update lock on the product before reading movements. Both movement directions use this protocol. Tests exercise the actual SQL Server implementation with independent contexts.

Sequence: scaffold and references; domain/application; SQL persistence and migrations; Razor UI; unit and SQL integration tests; real HTTP/browser verification; documentation and final build.

Edge cases: trimmed case-insensitive SKU uniqueness, empty histories, stock equal to reorder level, inactive/missing products, invalid enums and quantities, duplicate submissions, simultaneous issues, large aggregate totals, database conflicts and escaped notes.
