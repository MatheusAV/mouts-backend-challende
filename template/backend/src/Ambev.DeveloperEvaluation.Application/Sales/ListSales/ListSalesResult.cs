namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesResult
{
    public IEnumerable<ListSaleItemSummary> Sales { get; set; } = Enumerable.Empty<ListSaleItemSummary>();
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ListSaleItemSummary
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
