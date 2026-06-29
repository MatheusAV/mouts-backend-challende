namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

public class ListSalesResponse
{
    public IEnumerable<SaleSummaryResponse> Sales { get; set; } = Enumerable.Empty<SaleSummaryResponse>();
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class SaleSummaryResponse
{
    public Guid Id { get; set; }
    public string SaleNumber { get; set; } = string.Empty;
    public DateTime SaleDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public bool IsCancelled { get; set; }
    public int ItemCount { get; set; }
}
