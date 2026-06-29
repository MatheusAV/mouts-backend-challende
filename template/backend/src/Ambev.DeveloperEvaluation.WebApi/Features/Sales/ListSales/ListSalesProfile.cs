using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Sales.ListSales;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.ListSales;

public class ListSalesProfile : Profile
{
    public ListSalesProfile()
    {
        CreateMap<ListSalesResult, ListSalesResponse>();
        CreateMap<ListSaleItemSummary, SaleSummaryResponse>();
    }
}
