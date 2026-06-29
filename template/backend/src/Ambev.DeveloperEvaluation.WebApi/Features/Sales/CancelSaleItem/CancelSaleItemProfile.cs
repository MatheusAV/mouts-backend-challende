using AutoMapper;
using Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

namespace Ambev.DeveloperEvaluation.WebApi.Features.Sales.CancelSaleItem;

public class CancelSaleItemProfile : Profile
{
    public CancelSaleItemProfile()
    {
        CreateMap<CancelSaleItemResult, CancelSaleItemResponse>();
    }
}
