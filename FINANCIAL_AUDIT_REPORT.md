# Financial Accuracy and Database Transaction Integrity Audit Report

## 1. Monetary types and problems found

### Backend (ASP.NET Core / EF Core)
- All monetary entities and DTOs use C# `decimal` (`Account.Balance`, `Income.Amount`, `Expense.Amount`, `Transfer.Amount`, `Budget.BudgetAmount`, `BudgetCategory.AllocatedAmount`, `SavingsGoal.TargetAmount` / `SavedAmount`, `Transaction.Amount`, `RecurringTransaction.Amount`).
- No backend monetary field uses `float` or `double`. The only `double` usages are pagination calculations and DTO `[Range]` validation bounds, which do not participate in stored monetary values.
- EF Core maps `decimal` to SQL Server `decimal(18,2)` by default. This is appropriate for currency-scale values and avoids binary floating-point storage.
- JSON serialization uses System.Text.Json, which serializes `decimal` as a JSON number. Angular parses it into a JavaScript number. For normal currency magnitudes this is acceptable; very large values beyond safe integer precision are not expected for this application.
- Backend calculations use decimal arithmetic, including dashboard income/expense totals, monthly summaries, budget-vs-expense, savings progress, and transfer updates.
- Angular uses JavaScript `number` (IEEE-754) for client-side totals, percentages, and chart scaling. The main monetary authority remains the backend. The frontend rounds contribution updates to two decimals (`Math.round((currentSaved + amount) * 100) / 100`) and percentages with `Math.round`, which is acceptable for display but is not arbitrary-precision decimal arithmetic.

### Problems found
- No incorrect monetary type was found.
- No floating-point monetary calculation was found in backend financial logic.
- No evidence was found of inconsistent rounding in backend aggregate queries.
- The main financial risk is not data type precision but concurrency and multi-record consistency, described below.

## 2. Database transaction boundaries reviewed

Reviewed operations:
- `IncomesController.CreateIncome`, `UpdateIncome`, `DeleteIncome`
- `ExpensesController.CreateExpense`, `UpdateExpense`, `DeleteExpense`
- `TransfersController.CreateTransfer`, `UpdateTransfer`, `DeleteTransfer`
- `AccountsController.CreateAccount`, `UpdateAccount`, `DeleteAccount`
- `BudgetsController` and `BudgetCategoriesController` create/update/delete
- `SavingsGoalsController` create/update/delete
- `RecurringTransactionsController` create/update/delete
- `TransactionsController` create/update/delete (no account-balance side effects)
- Dashboard and summary aggregate queries

Findings:
- Each financial write that changes a transaction record and related account balance is performed in a single `SaveChangesAsync()` call.
- EF Core wraps a single `SaveChanges()` call in a database transaction for relational providers, so those operations are atomic.
- No explicit transaction was added because no reviewed operation requires multiple `SaveChanges()` calls or mixed non-database operations to be atomic.
- Read-only summary endpoints use grouped aggregate queries and do not modify data.

## 3. Changes made and reason for each

### Tests added
- `FinancialDecimalAndExpenseTests.cs`
  - Verifies decimal aggregate totals are preserved exactly through income, expense, and dashboard summary endpoints.
  - Verifies expense create, update, account change, and delete maintain account balances without double counting.
- `FinancialIncomeTransferFailureTests.cs`
  - Verifies income update/delete maintain account balances.
  - Verifies transfer create, update, account change, and delete maintain both accounts.
  - Verifies failed expense/transfer updates do not persist partial account-balance changes.

### Code changes
- No production financial calculation logic was changed.
- No schema changes were executed.
- No explicit transactions were added, because single `SaveChanges()` already provides the required atomicity for the reviewed operations.

## 4. Tests added, passed, and failed

- Existing tests: 18
- New tests: 5
- Total tests: 23
- Result: 23 passed, 0 failed
- New tests cover decimal totals, expense balance changes, income balance changes, transfer balance changes, update account changes, delete reversal, and failed-operation rollback at the API/database boundary.
- Concurrency behavior was not tested because the in-memory SQLite test environment and single-process test runner cannot reliably reproduce concurrent account updates.

## 5. Build results

- Backend build: succeeded, 0 warnings, 0 errors.
- Backend tests: succeeded, 23/23 passed.
- Frontend build: succeeded. Only Angular template warnings were reported; no build errors.

## 6. SQL scripts requiring manual review

- `Budget-Tracking-Backend/BudgetTracker.API/Data/PerformanceIndexes.sql`
  - Review-only script from the prior performance work.
  - It skips missing tables and does not execute schema changes automatically.
  - No new SQL script was required for this audit because monetary precision already uses `decimal(18,2)`.
- If future production hardening is desired, a DBA should review and apply:
  - A unique constraint/index on `(UserId, BudgetYear, BudgetMonth)` for `Budgets`.
  - Concurrency control for `Accounts.Balance`, such as a `rowversion` column or conditional balance updates.

## 7. Remaining risks, especially concurrency and schema limitations

- Concurrent requests can update the same account balance without optimistic concurrency or conditional updates. The current model has no `RowVersion`/concurrency token and controllers perform read-modify-write balance updates. This is the primary remaining integrity risk.
- Account deletion checks only `Balance == 0`. It does not check for related income, expense, transfer, or recurring-transaction records. Related records may be cascade-deleted by EF conventions, and counterpart account balances from historical transfers are not reversed by account deletion.
- Budget allocation does not enforce that total allocated amounts cannot exceed the budget amount at the database level.
- Savings goals are updated independently and do not interact with account balances.
- Recurring transactions are stored as definitions only; no workflow generates financial entries or updates balances.
- Tests use SQLite in-memory via `EnsureCreated()`, not SQL Server. SQL Server-specific precision, locking, and concurrency behavior were reviewed by code inspection only, not executed.
- No production database backup was available, so no live SQL Server execution plans or schema inspection were performed.

