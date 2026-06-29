using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

/// <summary>
/// Trata o evento de item de venda cancelado.
/// SRP: responsabilidade única de reagir ao ItemCancelledEvent.
/// </summary>
public sealed class ItemCancelledEventHandler : INotificationHandler<ItemCancelledEvent>
{
    private readonly ILogger<ItemCancelledEventHandler> _logger;

    public ItemCancelledEventHandler(ILogger<ItemCancelledEventHandler> logger)
        => _logger = logger;

    public Task Handle(ItemCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Evento: ItemCancelled] SaleId={SaleId} ItemId={ItemId} ProdutoId={ProductId} Produto={Product}",
            notification.SaleId, notification.ItemId, notification.ProductId, notification.ProductName);

        return Task.CompletedTask;
    }
}
