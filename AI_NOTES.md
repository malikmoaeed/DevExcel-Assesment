# AI usage notes

Tool used: OpenAI Codex in the desktop app.

Codex read all four assessment pages, scaffolded the .NET 10 solution, implemented domain/application/infrastructure/MVC layers, generated EF migrations and seed data, wrote Bootstrap/Razor views and xUnit tests, and ran builds and database checks. It also drafted documentation and local commits. No remote push was performed. The candidate remains responsible for reviewing and understanding every submitted line.

## Actual issues caught during implementation

The initial repository applied filtering/sorting after projecting into a positional record DTO. It compiled, but SQL Server integration tests exposed an EF Core translation failure, including a 500 on the list page. Filtering and sorting were moved onto the entity query before the DTO projection. This is a concrete example of why compiling and testing with a fake repository alone is insufficient.

The initial AI-selected EF Core 10.0.4 dependency set pulled an older cryptography package with reported vulnerabilities. NuGet restore failed because warnings were treated as errors. The dependencies were updated to stable 10.0.14 packages, with a patched explicit cryptography reference. The subsequent full build succeeded with zero warnings/errors.

An execution-order mistake also started the web app while tests were still rebuilding it, causing Windows DLL file-lock errors. The app was stopped and the tests were rerun. This was a verification workflow issue, not a claim of a passing test run.

The HTTP test initially overrode configuration too late for the already registered DbContext and wrote its WEB-TEST record to the demo database. The test was corrected to explicitly replace the DbContext registration, assert the actual test database name, and check the final ledger row count. The single test-created demo product and its two movements were removed; the five original sample products were retained.

See `VERIFICATION.md` for final observed results; intermediate failures are not counted as successful verification.

## Candidate review — complete before submission

**Candidate to complete with an actual example after reviewing the generated code:** describe something AI got wrong that you personally identified, how you corrected it, and what evidence verified the correction. Do not claim the examples above as your own discovery unless you independently reviewed them.

Be ready to explain why UPDLOCK is acquired before reading the movement sum, why both movement directions use the same locking protocol, why uniqueness needs a database index even with a precheck, how cancellation rolls back the transaction, and why an idempotency key differs from a business order reference.
