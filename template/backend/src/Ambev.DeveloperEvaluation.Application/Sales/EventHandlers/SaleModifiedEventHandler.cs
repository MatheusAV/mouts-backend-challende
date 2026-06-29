using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

/// <summary>
/// Trata o evento de venda modificada.
/// SRP: responsabilidade única de reagir ao SaleModifiedEvent.
/// </summary>
public sealed class SaleModifiedEventHandler : INotificationHandler<SaleModifiedEvent>
{
    private readonly ILogger<SaleModifiedEventHandler> _logger;

    public SaleModifiedEventHandler(ILogger<SaleModifiedEventHandler> logger)
        => _logger = logger;

    public Task Handle(SaleModifiedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Evento: SaleModified] SaleId={SaleId} Número={SaleNumber} Total={Total:C}",
            notification.SaleId, notification.SaleNumber, notification.TotalAmount);

        return Task.CompletedTask;
    }
}
