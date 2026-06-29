using MediatR;
using Microsoft.Extensions.Logging;
using Ambev.DeveloperEvaluation.Domain.Events;

namespace Ambev.DeveloperEvaluation.Application.Sales.EventHandlers;

/// <summary>
/// Trata o evento de venda criada.
/// SRP: responsabilidade única de reagir ao SaleCreatedEvent.
/// </summary>
public sealed class SaleCreatedEventHandler : INotificationHandler<SaleCreatedEvent>
{
    private readonly ILogger<SaleCreatedEventHandler> _logger;

    public SaleCreatedEventHandler(ILogger<SaleCreatedEventHandler> logger)
        => _logger = logger;

    public Task Handle(SaleCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Evento: SaleCreated] SaleId={SaleId} Número={SaleNumber} ClienteId={CustomerId} Total={Total:C}",
            notification.SaleId, notification.SaleNumber, notification.CustomerId, notification.TotalAmount);

        return Task.CompletedTask;
    }
}
