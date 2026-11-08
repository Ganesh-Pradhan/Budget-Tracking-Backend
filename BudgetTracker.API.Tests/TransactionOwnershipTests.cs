using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class TransactionOwnershipTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TransactionOwnershipTests(CustomWebApplicationFactory factory)
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
    public async Task Transaction_CreatedByUserA_CannotBeUpdatedOrDeletedByUserB()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var categoryResponse = await clientA.PostAsJsonAsync("/api/categories", new
        {
            categoryName = "Transaction Category",
            categoryType = "Expense",
            isActive = true
        });
        categoryResponse.EnsureSuccessStatusCode();
        using var categoryDoc = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryDoc.RootElement.GetProperty("categoryId").GetInt32();

        var paymentModeResponse = await clientA.PostAsJsonAsync("/api/paymentmodes", new
        {
            modeName = "Transaction Mode",
            isActive = true
        });
        paymentModeResponse.EnsureSuccessStatusCode();
        using var paymentModeDoc = JsonDocument.Parse(await paymentModeResponse.Content.ReadAsStringAsync());
        var paymentModeId = paymentModeDoc.RootElement.GetProperty("paymentModeId").GetInt32();

        var createResponse = await clientA.PostAsJsonAsync("/api/transactions", new
        {
            categoryId,
            paymentModeId,
            transactionType = "Expense",
            amount = 10m,
            transactionDate = DateTime.UtcNow,
            description = "A Transaction"
        });
        createResponse.EnsureSuccessStatusCode();

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var transactionId = created.RootElement.GetProperty("transactionId").GetInt32();

        var updateResponse = await clientB.PutAsJsonAsync($"/api/transactions/{transactionId}", new
        {
            categoryId,
            paymentModeId,
            transactionType = "Expense",
            amount = 20m,
            transactionDate = DateTime.UtcNow,
            description = "Hacked"
        });
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);

        var deleteResponse = await clientB.DeleteAsync($"/api/transactions/{transactionId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}