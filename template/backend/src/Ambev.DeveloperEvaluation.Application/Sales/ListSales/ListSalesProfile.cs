using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesProfile : Profile
{
    public ListSalesProfile()
    {
        CreateMap<Sale, ListSaleItemSummary>()
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.Items.Count));
    }
}
