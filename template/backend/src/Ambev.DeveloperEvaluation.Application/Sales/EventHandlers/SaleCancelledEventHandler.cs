using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

/// <summary>
/// Trata o evento de venda cancelada.
/// SRP: responsabilidade única de reagir ao SaleCancelledEvent.
/// </summary>
public sealed class SaleCancelledEventHandler : INotificationHandler<SaleCancelledEvent>
{
    private readonly ILogger<SaleCancelledEventHandler> _logger;

    public SaleCancelledEventHandler(ILogger<SaleCancelledEventHandler> logger)
        => _logger = logger;

    public Task Handle(SaleCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Evento: SaleCancelled] SaleId={SaleId} Número={SaleNumber}",
            notification.SaleId, notification.SaleNumber);

        return Task.CompletedTask;
    }
}
