namespace BudgetTracker.API.DTOs;

public class PaginatedResult<T>
{
    public int TotalRecords { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public List<T> Data { get; set; } = new();
}