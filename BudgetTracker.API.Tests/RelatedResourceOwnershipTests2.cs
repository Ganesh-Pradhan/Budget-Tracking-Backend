using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class RelatedResourceOwnershipTests2 : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RelatedResourceOwnershipTests2(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<(HttpClient Client, int UserId, string Token)> RegisterUserAsync(string email)
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
        var userId = document.RootElement.GetProperty("data").GetProperty("userId").GetInt32();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, userId, token);
    }

    [Fact]
    public async Task RecurringTransaction_CannotReferenceAnotherUsersAccount()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var accountResponse = await clientA.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "A Account",
            balance = 100m,
            isActive = true
        });
        accountResponse.EnsureSuccessStatusCode();
        using var accountDoc = JsonDocument.Parse(await accountResponse.Content.ReadAsStringAsync());
        var accountId = accountDoc.RootElement.GetProperty("accountId").GetInt32();

        var categoryResponse = await clientB.PostAsJsonAsync("/api/categories", new
        {
            categoryName = "B Category",
            categoryType = "Expense",
            isActive = true
        });
        categoryResponse.EnsureSuccessStatusCode();
        using var categoryDoc = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryDoc.RootElement.GetProperty("categoryId").GetInt32();

        var paymentModeResponse = await clientB.PostAsJsonAsync("/api/paymentmodes", new
        {
            modeName = "B Mode",
            isActive = true
        });
        paymentModeResponse.EnsureSuccessStatusCode();
        using var paymentModeDoc = JsonDocument.Parse(await paymentModeResponse.Content.ReadAsStringAsync());
        var paymentModeId = paymentModeDoc.RootElement.GetProperty("paymentModeId").GetInt32();

        var createResponse = await clientB.PostAsJsonAsync("/api/recurringtransactions", new
        {
            accountId,
            categoryId,
            paymentModeId,
            transactionType = "Expense",
            amount = 10m,
            description = "cross-user",
            frequency = "Monthly",
            startDate = DateTime.UtcNow,
            endDate = (DateTime?)null,
            nextOccurrenceDate = DateTime.UtcNow,
            isActive = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
    }

    [Fact]
    public async Task Income_CannotReferenceAnotherUsersAccount()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var accountResponse = await clientA.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "A Account",
            balance = 100m,
            isActive = true
        });
        accountResponse.EnsureSuccessStatusCode();
        using var accountDoc = JsonDocument.Parse(await accountResponse.Content.ReadAsStringAsync());
        var accountId = accountDoc.RootElement.GetProperty("accountId").GetInt32();

        var sourceResponse = await clientB.PostAsJsonAsync("/api/incomesources", new
        {
            sourceName = "B Source",
            isActive = true
        });
        sourceResponse.EnsureSuccessStatusCode();
        using var sourceDoc = JsonDocument.Parse(await sourceResponse.Content.ReadAsStringAsync());
        var sourceId = sourceDoc.RootElement.GetProperty("sourceId").GetInt32();

        var createResponse = await clientB.PostAsJsonAsync("/api/incomes", new
        {
            accountId,
            sourceId,
            amount = 10m,
            incomeDate = DateTime.UtcNow,
            description = "cross-user"
        });

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
    }
}