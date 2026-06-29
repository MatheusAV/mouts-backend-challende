using MediatR;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;

namespace Ambev.DeveloperEvaluation.Application.Sales.CancelSaleItem;

public class CancelSaleItemHandler : IRequestHandler<CancelSaleItemCommand, CancelSaleItemResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMediator _mediator;

    public CancelSaleItemHandler(ISaleRepository saleRepository, IMediator mediator)
    {
        _saleRepository = saleRepository;
        _mediator = mediator;
    }

    public async Task<CancelSaleItemResult> Handle(CancelSaleItemCommand command, CancellationToken cancellationToken)
    {
        var sale = await _saleRepository.GetByIdAsync(command.SaleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Sale {command.SaleId} not found.");

        if (sale.IsCancelled)
            throw new InvalidOperationException("Cannot cancel an item from a cancelled sale.");

        sale.CancelItem(command.ItemId);
        await _saleRepository.UpdateAsync(sale, cancellationToken);

        var item = sale.Items.First(i => i.Id == command.ItemId);
        await _mediator.Publish(new ItemCancelledEvent(sale.Id, item.Id, item.ProductId, item.ProductName), cancellationToken);

        return new CancelSaleItemResult { SaleId = sale.Id, ItemId = item.Id, IsCancelled = true };
    }
}
