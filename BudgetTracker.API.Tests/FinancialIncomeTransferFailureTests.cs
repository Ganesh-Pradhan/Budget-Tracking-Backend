using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class FinancialIncomeTransferFailureTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    public FinancialIncomeTransferFailureTests(CustomWebApplicationFactory factory) => _factory = factory;

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

    private static async Task<int> IncomeSource(HttpClient c)
    {
        var r = await c.PostAsJsonAsync("/api/incomesources", new { sourceName = $"IS{Guid.NewGuid():N}", isActive = true });
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("sourceId").GetInt32();
    }

    private static async Task<decimal> Balance(HttpClient c, int id)
    {
        var r = await c.GetAsync($"/api/accounts/{id}");
        r.EnsureSuccessStatusCode();
        using var d = JsonDocument.Parse(await r.Content.ReadAsStringAsync());
        return d.RootElement.GetProperty("balance").GetDecimal();
    }

    [Fact]
    public async Task IncomeUpdateAndDelete_MaintainBalance()
    {
        var c = await RegisterAsync($"inc{Guid.NewGuid():N}@example.com");
        var a = await Account(c, 0m);
        var b = await Account(c, 0m);
        var s = await IncomeSource(c);
        var cr = await c.PostAsJsonAsync("/api/incomes", new { accountId = a, sourceId = s, amount = 10m, incomeDate = DateTime.UtcNow, description = "i" });
        cr.EnsureSuccessStatusCode();
        using var cd = JsonDocument.Parse(await cr.Content.ReadAsStringAsync());
        var id = cd.RootElement.GetProperty("incomeId").GetInt32();
        var ur = await c.PutAsJsonAsync($"/api/incomes/{id}", new { accountId = b, sourceId = s, amount = 15m, incomeDate = DateTime.UtcNow, description = "u" });
        ur.EnsureSuccessStatusCode();
        Assert.Equal(0m, await Balance(c, a));
        Assert.Equal(15m, await Balance(c, b));
        var dr = await c.DeleteAsync($"/api/incomes/{id}");
        Assert.Equal(HttpStatusCode.OK, dr.StatusCode);
        Assert.Equal(0m, await Balance(c, b));
    }

    [Fact]
    public async Task TransferCreateUpdateDelete_MaintainBalances()
    {
        var c = await RegisterAsync($"tr{Guid.NewGuid():N}@example.com");
        var a = await Account(c, 100m);
        var b = await Account(c, 0m);
        var d = await Account(c, 0m);
        var cr = await c.PostAsJsonAsync("/api/transfers", new { fromAccountId = a, toAccountId = b, amount = 40m, transferDate = DateTime.UtcNow, description = "t" });
        cr.EnsureSuccessStatusCode();
        using var cd = JsonDocument.Parse(await cr.Content.ReadAsStringAsync());
        var id = cd.RootElement.GetProperty("transferId").GetInt32();
        Assert.Equal(60m, await Balance(c, a));
        Assert.Equal(40m, await Balance(c, b));
        var ur = await c.PutAsJsonAsync($"/api/transfers/{id}", new { fromAccountId = a, toAccountId = d, amount = 10m, transferDate = DateTime.UtcNow, description = "u" });
        ur.EnsureSuccessStatusCode();
        Assert.Equal(90m, await Balance(c, a));
        Assert.Equal(0m, await Balance(c, b));
        Assert.Equal(10m, await Balance(c, d));
        var dr = await c.DeleteAsync($"/api/transfers/{id}");
        Assert.Equal(HttpStatusCode.OK, dr.StatusCode);
        Assert.Equal(100m, await Balance(c, a));
        Assert.Equal(0m, await Balance(c, d));
    }

    [Fact]
    public async Task FailedTransferUpdate_DoesNotPersistPartialBalance()
    {
        var c = await RegisterAsync($"fail{Guid.NewGuid():N}@example.com");
        var a = await Account(c, 100m);
        var b = await Account(c, 0m);
        var d = await Account(c, 0m);
        var tr = await c.PostAsJsonAsync("/api/transfers", new { fromAccountId = a, toAccountId = b, amount = 40m, transferDate = DateTime.UtcNow, description = "t" });
        tr.EnsureSuccessStatusCode();
        using var td = JsonDocument.Parse(await tr.Content.ReadAsStringAsync());
        var tid = td.RootElement.GetProperty("transferId").GetInt32();
        var r2 = await c.PutAsJsonAsync($"/api/transfers/{tid}", new { fromAccountId = a, toAccountId = d, amount = 200m, transferDate = DateTime.UtcNow, description = "too large" });
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        Assert.Equal(60m, await Balance(c, a));
        Assert.Equal(40m, await Balance(c, b));
        Assert.Equal(0m, await Balance(c, d));
    }
}
