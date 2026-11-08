using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class RecurringOwnershipTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public RecurringOwnershipTests(CustomWebApplicationFactory factory)
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
    public async Task RecurringTransaction_CreatedByUserA_CannotBeAccessedByUserB()
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

        var categoryResponse = await clientA.PostAsJsonAsync("/api/categories", new
        {
            categoryName = "Recurring Category",
            categoryType = "Expense",
            isActive = true
        });
        categoryResponse.EnsureSuccessStatusCode();
        using var categoryDoc = JsonDocument.Parse(await categoryResponse.Content.ReadAsStringAsync());
        var categoryId = categoryDoc.RootElement.GetProperty("categoryId").GetInt32();

        var paymentModeResponse = await clientA.PostAsJsonAsync("/api/paymentmodes", new
        {
            modeName = "Recurring Mode",
            isActive = true
        });
        paymentModeResponse.EnsureSuccessStatusCode();
        using var paymentModeDoc = JsonDocument.Parse(await paymentModeResponse.Content.ReadAsStringAsync());
        var paymentModeId = paymentModeDoc.RootElement.GetProperty("paymentModeId").GetInt32();

        var createResponse = await clientA.PostAsJsonAsync("/api/recurringtransactions", new
        {
            accountId,
            categoryId,
            paymentModeId,
            transactionType = "Expense",
            amount = 10m,
            description = "A Recurring",
            frequency = "Monthly",
            startDate = DateTime.UtcNow,
            endDate = (DateTime?)null,
            nextOccurrenceDate = DateTime.UtcNow,
            isActive = true
        });
        createResponse.EnsureSuccessStatusCode();

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var recurringId = created.RootElement.GetProperty("recurringTransactionId").GetInt32();

        var getResponse = await clientB.GetAsync($"/api/recurringtransactions/{recurringId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var updateResponse = await clientB.PutAsJsonAsync($"/api/recurringtransactions/{recurringId}", new
        {
            transactionType = "Expense",
            amount = 20m,
            description = "Hacked",
            frequency = "Monthly",
            startDate = DateTime.UtcNow,
            endDate = (DateTime?)null,
            nextOccurrenceDate = DateTime.UtcNow,
            isActive = true
        });
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);

        var deleteResponse = await clientB.DeleteAsync($"/api/recurringtransactions/{recurringId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}