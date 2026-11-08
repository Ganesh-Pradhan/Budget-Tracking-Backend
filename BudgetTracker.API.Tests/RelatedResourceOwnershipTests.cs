using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class RelatedResourceOwnershipTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RelatedResourceOwnershipTests(CustomWebApplicationFactory factory)
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
    public async Task BudgetCategory_CannotReferenceAnotherUsersBudget()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var budgetResponse = await clientA.PostAsJsonAsync("/api/budgets", new
        {
            budgetYear = 2027,
            budgetMonth = 2,
            budgetAmount = 100m
        });
        budgetResponse.EnsureSuccessStatusCode();
        using var budgetDoc = JsonDocument.Parse(await budgetResponse.Content.ReadAsStringAsync());
        var budgetId = budgetDoc.RootElement.GetProperty("budgetId").GetInt32();

        var categoryResponse = await clientA.PostAsJsonAsync("/api/categories", new
        {
            categoryName = "Budget Category",
            categoryType = "Expense",
            isActive = true
        });
        categoryResponse.EnsureSuccessStatusCode();
        using var categoryDoc = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryDoc.RootElement.GetProperty("categoryId").GetInt32();

        var createResponse = await clientB.PostAsJsonAsync("/api/budgetcategories", new
        {
            budgetId,
            categoryId,
            allocatedAmount = 10m
        });

        Assert.Equal(HttpStatusCode.BadRequest, createResponse.StatusCode);
    }

    [Fact]
    public async Task Transfer_UpdateCannotReferenceAnotherUsersAccount()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var foreignAccountResponse = await clientA.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "A Account",
            balance = 100m,
            isActive = true
        });
        foreignAccountResponse.EnsureSuccessStatusCode();
        using var foreignAccountDoc = JsonDocument.Parse(await foreignAccountResponse.Content.ReadAsStringAsync());
        var foreignAccountId = foreignAccountDoc.RootElement.GetProperty("accountId").GetInt32();

        var fromAccountResponse = await clientB.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "B From",
            balance = 100m,
            isActive = true
        });
        fromAccountResponse.EnsureSuccessStatusCode();
        using var fromAccountDoc = JsonDocument.Parse(await fromAccountResponse.Content.ReadAsStringAsync());
        var fromAccountId = fromAccountDoc.RootElement.GetProperty("accountId").GetInt32();

        var toAccountResponse = await clientB.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "B To",
            balance = 0m,
            isActive = true
        });
        toAccountResponse.EnsureSuccessStatusCode();
        using var toAccountDoc = JsonDocument.Parse(await toAccountResponse.Content.ReadAsStringAsync());
        var toAccountId = toAccountDoc.RootElement.GetProperty("accountId").GetInt32();

        var createTransferResponse = await clientB.PostAsJsonAsync("/api/transfers", new
        {
            fromAccountId,
            toAccountId,
            amount = 10m,
            transferDate = DateTime.UtcNow,
            description = "B Transfer"
        });
        createTransferResponse.EnsureSuccessStatusCode();
        using var transferDoc = JsonDocument.Parse(await createTransferResponse.Content.ReadAsStringAsync());
        var transferId = transferDoc.RootElement.GetProperty("transferId").GetInt32();

        var updateResponse = await clientB.PutAsJsonAsync($"/api/transfers/{transferId}", new
        {
            fromAccountId,
            toAccountId = foreignAccountId,
            amount = 5m,
            transferDate = DateTime.UtcNow,
            description = "cross-user update"
        });

        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }
}