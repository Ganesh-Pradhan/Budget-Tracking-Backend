# Security configuration

## JWT signing key
Required configuration name: `Jwt:Key` (environment variable: `Jwt__Key` or `JWT_KEY`).

- The key must be at least 32 characters/bytes and must not be the committed placeholder.
- Do not commit a real production signing key. Use environment-specific configuration, user secrets, or a secret store.
- Local development: copy `appsettings.Development.example.json` to `appsettings.Development.json` (this file is gitignored) and replace the key with a strong random value, for example:

  PowerShell:
  ```
  [Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
  ```

  Or use `openssl rand -base64 32`.

## JWT issuer and audience
Configuration names: `Jwt:Issuer` and `Jwt:Audience`.

- Token generation and validation both use these values.
- Defaults are `BudgetTrackerAPI` and `BudgetTrackerClient`.
- Production deployments should set both explicitly.
- Because issuer and audience validation was enabled, tokens issued before this change (without `iss`/`aud`) are no longer accepted. Users must sign in again after deployment.

## Rate limiting
- The `auth` policy is now partitioned by remote IP address.
- Limit: 10 requests per minute per IP for authentication endpoints.

## Authorization audit (multi-user isolation)

Reviewed controllers: Accounts, Incomes, Expenses, Transfers, Budgets, BudgetCategories, SavingsGoals, RecurringTransactions, Transactions, Dashboard, Categories, IncomeSources, PaymentModes.

Findings:
- User-owned resources are scoped with `CurrentUserId`, which is read from the validated JWT `nameidentifier` claim.
- No DTO accepts a client-supplied user ID as proof of ownership.
- Related account and budget references are checked against the authenticated user in create/update paths.
- Fixed `TransfersController.UpdateTransfer`: `FromAccountId` and `ToAccountId` are now resolved with the authenticated user's ID, preventing a transfer update from affecting another user's accounts.
- `IncomesController.UpdateIncome` now resolves the replacement account with the authenticated user's ID, avoiding account-existence probing and cross-user account use.
- Lookup data (`Categories`, `IncomeSources`, `PaymentModes`) has no owner/admin model in the current schema or authentication claims. Their intended ownership remains ambiguous; no schema or authorization change was made.

Automated security regression tests now cover:
- Unauthenticated access to protected endpoints.
- Invalid/tampered JWT rejection.
- Cross-user account read/update/delete.
- Cross-user expense account reference.
- Cross-user transfer account reference on create and update.
- Cross-user budget read/update/delete.
- Cross-user savings goal read/update/delete.
- Cross-user recurring transaction read/update/delete.
- Cross-user transaction update/delete.
- Cross-user budget category creation with another user's budget.
- Cross-user recurring transaction creation with another user's account.
- Cross-user income creation with another user's account.

Remaining untested or partially tested scenarios:
- Cross-user income read/update/delete.
- Cross-user budget category read/update/delete.
- Dashboard and summary endpoints with two users.
- Budget category update with a foreign category/budget.
- Rate limiter behavior.
- Authenticated mutation of shared lookup data (policy undecided; see below).

## Shared lookup data policy decision

No code, documentation, seed data, or frontend behavior establishes whether Categories, IncomeSources, and PaymentModes are:
A. Shared reference data managed only by administrators, or
B. Personal records belonging to each user.

Current behavior treats them as shared, mutable reference data available to any authenticated user. Because the intended model is ambiguous, no breaking schema or authorization change was made.

Decision needed:
- If model A is selected, implement the smallest safe change as read-only reference data or admin-only writes. This requires an explicit admin identity/role model, provisioning workflow, and seed/import data; until then, ordinary users must not be able to mutate shared records.
- If model B is selected, add a `UserId` (or owner) column and foreign key to Categories, IncomeSources, and PaymentModes, scope all reads/writes by the authenticated user, provide a migration/backfill strategy for existing rows, and update frontend synchronization. A reviewable SQL script would be required before execution because database changes are not currently managed through EF migrations.

## Remaining risks
- JWT is stored in browser localStorage and is exposed to XSS. Moving to an HttpOnly cookie requires CSRF protection and is documented as a future migration, not implemented here.
- Registration still reveals whether an email is already registered.
- Password policy remains 6 characters for compatibility; consider stronger policy plus a reset flow.
- No server-side token revocation/blacklist is implemented; tokens expire after `Jwt:ExpiryHours` (default 24).
- Categories, IncomeSources, and PaymentModes remain globally mutable by any authenticated user until the lookup-data policy is decided.
No SQL script is included because no schema change was made.