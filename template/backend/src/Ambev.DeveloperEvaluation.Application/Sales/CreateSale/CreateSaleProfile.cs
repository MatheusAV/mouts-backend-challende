using AutoMapper;
using Ambev.DeveloperEvaluation.Domain.Entities;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

public class CreateSaleProfile : Profile
{
    public CreateSaleProfile()
    {
        CreateMap<CreateSaleCommand, Sale>()
            .ForMember(dest => dest.Items, opt => opt.Ignore());

        CreateMap<CreateSaleItemCommand, SaleItem>()
            .ForMember(dest => dest.Discount, opt => opt.Ignore())
            .ForMember(dest => dest.TotalAmount, opt => opt.Ignore());

        CreateMap<Sale, CreateSaleResult>();
        CreateMap<SaleItem, CreateSaleItemResult>();
    }
}
