using BudgetTracker.API.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add controllers
builder.Services.AddControllers();

// Allow the local Angular development server to call this API.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularLocal", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add Entity Framework Core
builder.Services.AddDbContext<BudgetTrackerDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString(
            "BudgetTrackerConnection")));

// Add OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure OpenAPI in Development
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AngularLocal");
app.MapControllers();

app.Run();