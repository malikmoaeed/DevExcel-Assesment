# Assessment compliance and verification

Verified on 5 November 2026 (Asia/Karachi), with .NET SDK 10.0.302 and real SQL Server LocalDB 17.0.4025.3.

## Final automated results

- `dotnet build --no-restore`: succeeded, **0 warnings, 0 errors**.
- `dotnet test --no-build --logger "trx;LogFilePrefix=final"`: **31 passed, 0 failed, 0 skipped** (Domain 15; Application 8; Infrastructure/MVC 8).
- `dotnet ef database update --project src/InventoryManagement.Infrastructure --startup-project src/InventoryManagement.Web`: applied InitialInventory successfully.
- `dotnet ef migrations has-pending-model-changes --no-build --project src/InventoryManagement.Infrastructure --startup-project src/InventoryManagement.Web`: no pending model changes.
- App started in Development and listened on `http://localhost:5080` with seeded LocalDB data.

TRX evidence is generated under each test project's ignored `TestResults` directory. The final runs are named `final_net10.0_20261105221221.trx` (Application), `final_net10.0_20261105221223.trx` (Domain), and `final_net10.0_20261105221237.trx` (Infrastructure).

| Requirement | Status | Verification |
|---|---|---|
| .NET 10 | Complete, tested | All seven projects target net10.0; successful SDK 10 build |
| C# | Complete, tested | Solution compiled with no warnings/errors |
| Clean Architecture | Complete, inspected | Domain has no project dependencies; Application references only Domain; Infrastructure implements application contract; Web composition uses DI |
| EF Core | Complete, tested | SQL Server 10.0.14 packages, applied migration, no pending model changes |
| Product CRUD | Complete, tested | MVC create/read/update and soft deactivation workflow; no hard-delete endpoint |
| Product search | Complete, tested | SQL and MVC tests search by SKU/name; unmatched query renders empty state |
| Stock In | Complete, tested | Application and HTTP workflow insert receipt and return derived stock |
| Stock Out | Complete, tested | Application and HTTP workflow issue stock; issue-to-zero succeeds |
| Stock calculation | Complete, tested | Empty, receipt-only, mixed movements, SQL totals and 64-bit overflow boundary tests |
| Concurrency protection | Complete, tested | Two independent SQL contexts compete to issue 7 of 10 units; exactly one succeeds, final stock 3, exactly two ledger rows including opening receipt |
| Negative stock prevention | Complete, tested | Insufficient request rejected with actual quantity; rollback and ledger row counts checked |
| Low-stock highlighting | Complete, tested | Domain boundary tests; HTTP output includes low-stock badge; browser shows Dell at equality and Samsung below threshold |
| Server-side validation | Complete, tested | Duplicate SKU, required/negative product fields, invalid quantities/type, missing/inactive products; HTTP anti-forgery and HTML encoding tests |
| Unit tests | Complete, tested | 23 unit test cases pass |
| Integration tests | Complete, tested | 7 SQL tests and 1 complete MVC workflow test pass against disposable LocalDB databases |
| Immutable movement history | Complete, tested | Private setters, no edit/delete actions, context deletion rejection test, newest-first history assertion |
| Duplicate submissions | Complete, tested | Same request ID/payload replay produces one ledger row |
| README | Complete | Architecture, workflow, concurrency, setup, commands, seed data, assumptions, limitations and improvements documented |
| AI_NOTES | Complete | Actual observed mistakes documented; candidate's personal review section intentionally remains for the candidate |
| Git history | Complete | Real incremental local commits; no remote push |

## Browser check and limitation

Chrome loaded `/Products`, `/Products/Details/3`, and `/Products/Edit/3`. Browser accessibility content confirmed the five seed products, correct stock/reorder values, low-stock labels, newest-first movements, movement form and editable product fields without a stock editor. The deactivation confirmation dialog appeared with the expected warning.

Screenshot capture then timed out. Browser automation could not dismiss the confirmation because Chrome's window was minimized and the browser control channel stalled. **Pixel-level visual review and narrow-viewport screenshot verification are not claimed.** If the review tab remains open with that dialog, choose **Cancel**. Automated HTTP tests independently verify product creation/editing/deactivation and all movement workflows. Create/edit share the same Razor form and were both rendered in the automated test.

## Remaining candidate/deployment work

- Review the solution and complete the clearly marked personal example in AI_NOTES before submitting it as your assessment.
- Perform a final visual review at desktop/mobile widths; browser screenshot automation was unavailable in this session.
- Authentication, production hosting/security configuration, pagination and other optional features are documented future work, not claimed deliverables.

Intermediate failures (query translation, test isolation, assembly locks and assertions) were investigated and fixed; only the final successful run is counted above.
