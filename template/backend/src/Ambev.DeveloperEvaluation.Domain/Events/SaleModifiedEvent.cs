using MediatR;

namespace Ambev.DeveloperEvaluation.Domain.Events;

/// <summary>Published when an existing sale is modified.</summary>
public record SaleModifiedEvent(Guid SaleId, string SaleNumber, decimal TotalAmount) : INotification;
