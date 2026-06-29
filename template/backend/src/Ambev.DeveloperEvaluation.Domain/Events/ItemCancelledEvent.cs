using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Published when an individual item within a sale is cancelled.</summary>
public record ItemCancelledEvent(Guid SaleId, Guid ItemId, Guid ProductId, string ProductName) : INotification;
