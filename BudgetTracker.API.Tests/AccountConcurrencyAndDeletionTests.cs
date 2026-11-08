using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using BudgetTracker.API.Data;
using Xunit;

namespace BudgetTracker.API.Tests;

public class AccountConcurrencyAndDeletionTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public AccountConcurrencyAndDeletionTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> RegisterAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Concurrency Test User",
            email,
            password = "Password123!",
            confirmPassword = "Password123!"
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
            document.RootElement.GetProperty("data").GetProperty("token").GetString()!);
        return client;
    }

    private static async Task<int> Account(HttpClient c, decimal b)
    {
        var r = await c.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = $"A{Guid.NewGuid():N}",
            balance = b,
            isActive = true
        });
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("accountId").GetInt32();
    }

    private static async Task<int> ExpenseCategory(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/categories", new
        {
            categoryName = $"EC{Guid.NewGuid():N}",
            categoryType = "Expense",
            isActive = true
        });
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("categoryId").GetInt32();
    }

    private static async Task<int> IncomeSource(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/incomesources", new
        {
            sourceName = $"IS{Guid.NewGuid():N}",
            isActive = true
        });
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("sourceId").GetInt32();
    }

    private static async Task<int> PaymentMode(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/paymentmodes", new
        {
            modeName = $"PM{Guid.NewGuid():N}",
            isActive = true
        });
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("paymentModeId").GetInt32();
    }

    private static async Task<decimal> Balance(HttpClient c, int id)
    {
        var r = await c.GetAsync($"/api/accounts/{id}");
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("balance").GetDecimal();
    }

    [Fact]
    public async Task DeleteAccount_WithNonZeroBalance_ReturnsBadRequest()
    {
        var c = await RegisterAsync($"delbal{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 100m);
        var dr = await c.DeleteAsync($"/api/accounts/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, dr.StatusCode);
        Assert.Equal(100m, await Balance(c, id));
    }

    [Fact]
    public async Task DeleteAccount_WithRelatedIncome_ReturnsBadRequest()
    {
        var c = await RegisterAsync($"delinc{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 0m);
        var s = await IncomeSource(c);
        (await c.PostAsJsonAsync("/api/incomes", new { accountId = id, sourceId = s, amount = 10m, incomeDate = DateTime.UtcNow, description = "i" })).EnsureSuccessStatusCode();
        var dr = await c.DeleteAsync($"/api/accounts/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, dr.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/accounts/{id}")).StatusCode);
    }
    [Fact]
    public async Task DeleteAccount_WithRelatedExpense_ReturnsBadRequest()
    {
        var c = await RegisterAsync($"delexp{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 10m);
        var cat = await ExpenseCategory(c);
        var pm = await PaymentMode(c);
        (await c.PostAsJsonAsync("/api/expenses", new { accountId = id, categoryId = cat, paymentModeId = pm, amount = 10m, expenseDate = DateTime.UtcNow, description = "e" })).EnsureSuccessStatusCode();
        // Balance is now 0 but a related expense still exists.
        var dr = await c.DeleteAsync($"/api/accounts/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, dr.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/accounts/{id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithRelatedTransferAsFromAccount_ReturnsBadRequest()
    {
        var c = await RegisterAsync($"deltrf{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 100m);
        var b = await Account(c, 0m);
        (await c.PostAsJsonAsync("/api/transfers", new { fromAccountId = id, toAccountId = b, amount = 10m, transferDate = DateTime.UtcNow, description = "t" })).EnsureSuccessStatusCode();
        var dr = await c.DeleteAsync($"/api/accounts/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, dr.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/accounts/{id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithRelatedTransferAsToAccount_ReturnsBadRequest()
    {
        var c = await RegisterAsync($"deltrt{Guid.NewGuid():N}@example.com");
        var a = await Account(c, 100m);
        var id = await Account(c, 0m);
        (await c.PostAsJsonAsync("/api/transfers", new { fromAccountId = a, toAccountId = id, amount = 10m, transferDate = DateTime.UtcNow, description = "t" })).EnsureSuccessStatusCode();
        var dr = await c.DeleteAsync($"/api/accounts/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, dr.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/accounts/{id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithRelatedRecurringTransaction_ReturnsBadRequest()
    {
        var c = await RegisterAsync($"delrec{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 0m);
        var cat = await ExpenseCategory(c);
        var pm = await PaymentMode(c);
        var today = DateTime.UtcNow;
        (await c.PostAsJsonAsync("/api/recurringtransactions", new { accountId = id, categoryId = cat, paymentModeId = pm, transactionType = "Expense", amount = 10m, description = "r", frequency = "Monthly", startDate = today, endDate = (DateTime?)null, nextOccurrenceDate = today.AddMonths(1), isActive = true })).EnsureSuccessStatusCode();
        var dr = await c.DeleteAsync($"/api/accounts/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, dr.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/accounts/{id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithZeroBalanceAndNoRelatedRecords_DeletesSuccessfully()
    {
        var c = await RegisterAsync($"delok{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 0m);
        var dr = await c.DeleteAsync($"/api/accounts/{id}");
        Assert.Equal(HttpStatusCode.OK, dr.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/accounts/{id}")).StatusCode);
    }
    [Fact]
    public async Task ConcurrentAccountBalanceUpdates_DoNotLoseUpdates()
    {
        var c = await RegisterAsync($"conc{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 100m);

        // Capture the current RowVersion before issuing concurrent updates.
        var getResp = await c.GetAsync($"/api/accounts/{id}");
        getResp.EnsureSuccessStatusCode();
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        var rowVersion = getDoc.RootElement.GetProperty("rowVersion").GetString();

        var taskA = c.PutAsJsonAsync($"/api/accounts/{id}", new
        {
            accountType = "Bank",
            accountName = $"PA{Guid.NewGuid():N}",
            bankName = "Bank A",
            accountNumber = "123",
            balance = 50m,
            isActive = true,
            rowVersion
        });
        var taskB = c.PutAsJsonAsync($"/api/accounts/{id}", new
        {
            accountType = "Bank",
            accountName = $"PB{Guid.NewGuid():N}",
            bankName = "Bank B",
            accountNumber = "456",
            balance = 75m,
            isActive = true,
            rowVersion
        });
        var results = await Task.WhenAll(taskA, taskB);
        var successCount = results.Count(r => r.IsSuccessStatusCode);
        var conflictCount = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, successCount);
        Assert.Equal(1, conflictCount);
        var final = await Balance(c, id);
        Assert.True(final == 50m || final == 75m, $"Expected balance 50 or 75, got {final}");
    }

    [Fact]
    public async Task RowVersion_ConcurrencyConflictIsDetected()
    {
        var c = await RegisterAsync($"rowv{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 100m);
        using (var scope1 = _factory.Services.CreateScope())
        {
            var db1 = scope1.ServiceProvider.GetRequiredService<BudgetTrackerDbContext>();
            var held = await db1.Accounts.FirstAsync(a => a.AccountId == id);
            held.Balance = 150m;
            using (var scope2 = _factory.Services.CreateScope())
            {
                var db2 = scope2.ServiceProvider.GetRequiredService<BudgetTrackerDbContext>();
                var other = await db2.Accounts.FirstAsync(a => a.AccountId == id);
                other.Balance = 250m;
                // Simulate the controller's concurrency-token bump on a competing save.
                other.RowVersion = Guid.NewGuid().ToByteArray();
                await db2.SaveChangesAsync();
            }
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db1.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task AccountCreation_InitializesRowVersion()
    {
        var c = await RegisterAsync($"initrv{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 100m);

        var resp = await c.GetAsync($"/api/accounts/{id}");
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var rowVersion = doc.RootElement.GetProperty("rowVersion").GetString();

        Assert.False(string.IsNullOrEmpty(rowVersion), "RowVersion must not be empty after creation.");
        var bytes = Convert.FromBase64String(rowVersion);
        Assert.Equal(16, bytes.Length);
    }

    [Fact]
    public async Task AccountUpdate_ChangesRowVersion()
    {
        var c = await RegisterAsync($"upderv{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 100m);

        var getResp = await c.GetAsync($"/api/accounts/{id}");
        getResp.EnsureSuccessStatusCode();
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        var originalRowVersion = getDoc.RootElement.GetProperty("rowVersion").GetString();

        var putResp = await c.PutAsJsonAsync($"/api/accounts/{id}", new
        {
            accountType = "Bank",
            accountName = $"Updated{Guid.NewGuid():N}",
            bankName = "Bank",
            accountNumber = "123",
            balance = 200m,
            isActive = true,
            rowVersion = originalRowVersion
        });
        putResp.EnsureSuccessStatusCode();
        using var putDoc = JsonDocument.Parse(await putResp.Content.ReadAsStringAsync());
        var newRowVersion = putDoc.RootElement.GetProperty("rowVersion").GetString();

        Assert.NotEqual(originalRowVersion, newRowVersion);
    }

    [Fact]
    public async Task UpdateAccount_WithStaleRowVersion_Returns409()
    {
        var c = await RegisterAsync($"staleupd{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 100m);

        var getResp = await c.GetAsync($"/api/accounts/{id}");
        getResp.EnsureSuccessStatusCode();
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        var originalRowVersion = getDoc.RootElement.GetProperty("rowVersion").GetString();

        // Change the RowVersion directly in the database to simulate a concurrent update.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BudgetTrackerDbContext>();
            var acct = await db.Accounts.FirstAsync(a => a.AccountId == id);
            acct.Balance = 999m;
            acct.RowVersion = Guid.NewGuid().ToByteArray();
            await db.SaveChangesAsync();
        }

        // Attempting to update with the stale RowVersion should return 409.
        var putResp = await c.PutAsJsonAsync($"/api/accounts/{id}", new
        {
            accountType = "Bank",
            accountName = $"Stale{Guid.NewGuid():N}",
            bankName = "Bank",
            accountNumber = "123",
            balance = 200m,
            isActive = true,
            rowVersion = originalRowVersion
        });
        Assert.Equal(HttpStatusCode.Conflict, putResp.StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithStaleRowVersion_Returns409()
    {
        var c = await RegisterAsync($"staledel{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 0m);

        var getResp = await c.GetAsync($"/api/accounts/{id}");
        getResp.EnsureSuccessStatusCode();
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        var originalRowVersion = getDoc.RootElement.GetProperty("rowVersion").GetString();

        // Change the RowVersion directly in the database to simulate a concurrent update.
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BudgetTrackerDbContext>();
            var acct = await db.Accounts.FirstAsync(a => a.AccountId == id);
            acct.RowVersion = Guid.NewGuid().ToByteArray();
            await db.SaveChangesAsync();
        }

        // Attempting to delete with the stale RowVersion should return 409.
        var delResp = await c.DeleteAsync($"/api/accounts/{id}?rowVersion={originalRowVersion}");
        Assert.Equal(HttpStatusCode.Conflict, delResp.StatusCode);

        // Account should still exist.
        Assert.Equal(HttpStatusCode.OK, (await c.GetAsync($"/api/accounts/{id}")).StatusCode);
    }

    [Fact]
    public async Task DeleteAccount_WithRowVersion_DeletesSuccessfully()
    {
        var c = await RegisterAsync($"delrv{Guid.NewGuid():N}@example.com");
        var id = await Account(c, 0m);

        var getResp = await c.GetAsync($"/api/accounts/{id}");
        getResp.EnsureSuccessStatusCode();
        using var getDoc = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
        var rowVersion = getDoc.RootElement.GetProperty("rowVersion").GetString();

        var delResp = await c.DeleteAsync($"/api/accounts/{id}?rowVersion={rowVersion}");
        Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await c.GetAsync($"/api/accounts/{id}")).StatusCode);
    }
}
