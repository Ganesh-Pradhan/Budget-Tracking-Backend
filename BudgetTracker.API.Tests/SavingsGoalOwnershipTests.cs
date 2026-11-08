using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BudgetTracker.API.Tests;

public class SavingsGoalOwnershipTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SavingsGoalOwnershipTests(CustomWebApplicationFactory factory)
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
    public async Task SavingsGoal_CreatedByUserA_CannotBeAccessedByUserB()
    {
        var (clientA, _, _) = await RegisterUserAsync($"a{Guid.NewGuid():N}@example.com");
        var (clientB, _, _) = await RegisterUserAsync($"b{Guid.NewGuid():N}@example.com");

        var createResponse = await clientA.PostAsJsonAsync("/api/savingsgoals", new
        {
            goalName = "A Goal",
            targetAmount = 100m,
            savedAmount = 10m,
            targetDate = DateTime.UtcNow
        });
        createResponse.EnsureSuccessStatusCode();

        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var goalId = created.RootElement.GetProperty("goalId").GetInt32();

        var getResponse = await clientB.GetAsync($"/api/savingsgoals/{goalId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);

        var updateResponse = await clientB.PutAsJsonAsync($"/api/savingsgoals/{goalId}", new
        {
            goalName = "Hacked",
            targetAmount = 200m,
            savedAmount = 10m,
            targetDate = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);

        var deleteResponse = await clientB.DeleteAsync($"/api/savingsgoals/{goalId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}