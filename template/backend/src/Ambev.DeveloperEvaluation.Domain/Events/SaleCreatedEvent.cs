using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Published when a new sale is created.</summary>
public record SaleCreatedEvent(Guid SaleId, string SaleNumber, Guid CustomerId, decimal TotalAmount) : INotification;
