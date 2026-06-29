using AutoMapper;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.ListSales;

public class ListSalesHandler : IRequestHandler<ListSalesQuery, ListSalesResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;

    public ListSalesHandler(ISaleRepository saleRepository, IMapper mapper)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
    }

    public async Task<ListSalesResult> Handle(ListSalesQuery query, CancellationToken cancellationToken)
    {
        var sales = await _saleRepository.GetAllAsync(query.Page, query.PageSize, cancellationToken);

        return new ListSalesResult
        {
            Sales = _mapper.Map<IEnumerable<ListSaleItemSummary>>(sales),
            Page = query.Page,
            PageSize = query.PageSize
        };
    }
}
