using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class OwnershipTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OwnershipTests(CustomWebApplicationFactory factory)
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
    public async Task Account_CreatedByUserA_CannotBeReadByUserB()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var createResponse = await clientA.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "A Account",
            balance = 100m,
            isActive = true
        });
        createResponse.EnsureSuccessStatusCode();

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var accountId = created.RootElement.GetProperty("accountId").GetInt32();

        var getResponse = await clientB.GetAsync($"/api/accounts/{accountId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var updateResponse = await clientB.PutAsJsonAsync($"/api/accounts/{accountId}", new
        {
            accountType = "Bank",
            accountName = "Hacked",
            balance = 0m,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);

        var deleteResponse = await clientB.DeleteAsync($"/api/accounts/{accountId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task Expense_CannotReferenceAnotherUsersAccount()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var createAccountResponse = await clientA.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "A Account",
            balance = 100m,
            isActive = true
        });
        createAccountResponse.EnsureSuccessStatusCode();
        using var accountDoc = JsonDocument.Parse(await createAccountResponse.Content.ReadAsStringAsync());
        var accountId = accountDoc.RootElement.GetProperty("accountId").GetInt32();

        var createCategoryResponse = await clientA.PostAsJsonAsync("/api/categories", new
        {
            categoryName = "Test Expense",
            categoryType = "Expense",
            isActive = true
        });
        createCategoryResponse.EnsureSuccessStatusCode();
        using var categoryDoc = JsonDocument.Parse(await createCategoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryDoc.RootElement.GetProperty("categoryId").GetInt32();

        var createPaymentModeResponse = await clientA.PostAsJsonAsync("/api/paymentmodes", new
        {
            modeName = "Test Mode",
            isActive = true
        });
        createPaymentModeResponse.EnsureSuccessStatusCode();
        using var paymentModeDoc = JsonDocument.Parse(await createPaymentModeResponse.Content.ReadAsStringAsync());
        var paymentModeId = paymentModeDoc.RootElement.GetProperty("paymentModeId").GetInt32();

        var expenseResponse = await clientB.PostAsJsonAsync("/api/expenses", new
        {
            accountId,
            categoryId,
            paymentModeId,
            amount = 10m,
            expenseDate = DateTime.UtcNow,
            description = "cross-user"
        });

        Assert.Equal(HttpStatusCode.BadRequest, expenseResponse.StatusCode);
    }

    [Fact]
    public async Task Transfer_CannotReferenceAnotherUsersAccount()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var createAccountResponse = await clientA.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "A Account",
            balance = 100m,
            isActive = true
        });
        createAccountResponse.EnsureSuccessStatusCode();
        using var accountDoc = JsonDocument.Parse(await createAccountResponse.Content.ReadAsStringAsync());
        var accountId = accountDoc.RootElement.GetProperty("accountId").GetInt32();

        var ownAccountResponse = await clientB.PostAsJsonAsync("/api/accounts", new
        {
            accountType = "Bank",
            accountName = "B Account",
            balance = 100m,
            isActive = true
        });
        ownAccountResponse.EnsureSuccessStatusCode();
        using var ownAccountDoc = JsonDocument.Parse(await ownAccountResponse.Content.ReadAsStringAsync());
        var ownAccountId = ownAccountDoc.RootElement.GetProperty("accountId").GetInt32();

        var transferResponse = await clientB.PostAsJsonAsync("/api/transfers", new
        {
            fromAccountId = ownAccountId,
            toAccountId = accountId,
            amount = 10m,
            transferDate = DateTime.UtcNow,
            description = "cross-user"
        });

        Assert.Equal(HttpStatusCode.BadRequest, transferResponse.StatusCode);
    }
}

