using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Published when a sale is cancelled.</summary>
public record SaleCancelledEvent(Guid SaleId, string SaleNumber) : INotification;
