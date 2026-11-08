using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class FinancialDecimalAndExpenseTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public FinancialDecimalAndExpenseTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> RegisterAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new { fullName = "Financial Test User", email, password = "Password123!", confirmPassword = "Password123!" });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", document.RootElement.GetProperty("data").GetProperty("token").GetString()!);
        return client;
    }

    private static async Task<int> Account(HttpClient c, decimal b)
    {
        var r = await c.PostAsJsonAsync("/api/accounts", new { accountType = "Bank", accountName = $"A{Guid.NewGuid():N}", balance = b, isActive = true });
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("accountId").GetInt32();
    }

    private static async Task<int> ExpenseCategory(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/categories", new { categoryName = $"EC{Guid.NewGuid():N}", categoryType = "Expense", isActive = true });
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("categoryId").GetInt32();
    }

    private static async Task<int> IncomeSource(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/incomesources", new { sourceName = $"IS{Guid.NewGuid():N}", isActive = true });
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("sourceId").GetInt32();
    }

    private static async Task<int> PaymentMode(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/paymentmodes", new { modeName = $"PM{Guid.NewGuid():N}", isActive = true });
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
    public async Task DecimalTotals_ArePreserved()
    {
        var c = await RegisterAsync($"dec{Guid.NewGuid():N}@example.com");
        var a = await Account(c, 100m);
        var s = await IncomeSource(c);
        var cat = await ExpenseCategory(c);
        var pm = await PaymentMode(c);
        (await c.PostAsJsonAsync("/api/incomes", new { accountId = a, sourceId = s, amount = 10.10m, incomeDate = DateTime.UtcNow, description = "i1" })).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync("/api/incomes", new { accountId = a, sourceId = s, amount = 20.20m, incomeDate = DateTime.UtcNow, description = "i2" })).EnsureSuccessStatusCode();
        (await c.PostAsJsonAsync("/api/expenses", new { accountId = a, categoryId = cat, paymentModeId = pm, amount = 5.05m, expenseDate = DateTime.UtcNow, description = "e1" })).EnsureSuccessStatusCode();
        var r = await c.GetAsync("/api/dashboard/summary");
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        Assert.Equal(30.30m, d.RootElement.GetProperty("totalIncome").GetDecimal());
        Assert.Equal(5.05m, d.RootElement.GetProperty("totalExpenses").GetDecimal());
        Assert.Equal(25.25m, d.RootElement.GetProperty("balance").GetDecimal());
    }

    [Fact]
    public async Task ExpenseCreateUpdateDelete_MaintainBalance()
    {
        var c = await RegisterAsync($"exp{Guid.NewGuid():N}@example.com");
        var a = await Account(c, 100m);
        var b = await Account(c, 50m);
        var cat = await ExpenseCategory(c);
        var pm = await PaymentMode(c);
        var cr = await c.PostAsJsonAsync("/api/expenses", new { accountId = a, categoryId = cat, paymentModeId = pm, amount = 10m, expenseDate = DateTime.UtcNow, description = "e" });
        cr.EnsureSuccessStatusCode();
        using var cd = JsonDocument.Parse(await cr.Content.ReadAsStringAsync());
        var id = cd.RootElement.GetProperty("expenseId").GetInt32();
        Assert.Equal(90m, await Balance(c, a));
        var ur = await c.PutAsJsonAsync($"/api/expenses/{id}", new { accountId = b, categoryId = cat, paymentModeId = pm, amount = 25m, expenseDate = DateTime.UtcNow, description = "u" });
        ur.EnsureSuccessStatusCode();
        Assert.Equal(100m, await Balance(c, a));
        Assert.Equal(25m, await Balance(c, b));
        var dr = await c.DeleteAsync($"/api/expenses/{id}");
        Assert.Equal(HttpStatusCode.OK, dr.StatusCode);
        Assert.Equal(100m, await Balance(c, a));
        Assert.Equal(50m, await Balance(c, b));
    }
}
