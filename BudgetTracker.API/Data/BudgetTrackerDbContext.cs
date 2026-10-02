using BudgetTracker.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace BudgetTracker.API.Data;

public class BudgetTrackerDbContext : DbContext
{
    public BudgetTrackerDbContext(
        DbContextOptions<BudgetTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<PaymentMode> PaymentModes { get; set; }

    public DbSet<IncomeSource> IncomeSources { get; set; }

    public DbSet<Transaction> Transactions { get; set; }


    public DbSet<Income> Income { get; set; }

    public DbSet<Expense> Expenses { get; set; }

    public DbSet<Budget> Budgets { get; set; }

    public DbSet<BudgetCategory> BudgetCategories { get; set; }

    public DbSet<SavingsGoal> SavingsGoals { get; set; }

    public DbSet<Account> Accounts { get; set; }

    public DbSet<RecurringTransaction> RecurringTransactions { get; set; }

    public DbSet<Transfer> Transfers { get; set; }
}