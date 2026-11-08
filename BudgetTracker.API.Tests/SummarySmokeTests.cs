using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class SummarySmokeTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SummarySmokeTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> RegisterAsync(string email)
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            fullName = "Test User",
            email,
            password = "Password123!",
            confirmPassword = "Password123!"
        });
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("data").GetProperty("token").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task EmptyAccountSummary_ReturnsZeros()
    {
        var client = await RegisterAsync($"summary{Guid.NewGuid():N}@example.com");
        var response = await client.GetAsync("/api/accounts/summary");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(0, doc.RootElement.GetProperty("totalAccounts").GetInt32());
        Assert.Equal(0m, doc.RootElement.GetProperty("totalBalance").GetDecimal());
    }

    [Fact]
    public async Task EmptyRecurringAndTransferSummaries_ReturnZeros()
    {
        var client = await RegisterAsync($"summary2{Guid.NewGuid():N}@example.com");
        var recurringResponse = await client.GetAsync("/api/recurringtransactions/summary");
        Assert.Equal(HttpStatusCode.OK, recurringResponse.StatusCode);
        var transferResponse = await client.GetAsync("/api/transfers/summary");
        Assert.Equal(HttpStatusCode.OK, transferResponse.StatusCode);
    }

    [Fact]
    public async Task DashboardMonthlySummary_ReturnsTwelveMonths()
    {
        var client = await RegisterAsync($"summary3{Guid.NewGuid():N}@example.com");
        var response = await client.GetAsync("/api/dashboard/monthly-summary?year=2026");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(12, doc.RootElement.GetArrayLength());
    }
}