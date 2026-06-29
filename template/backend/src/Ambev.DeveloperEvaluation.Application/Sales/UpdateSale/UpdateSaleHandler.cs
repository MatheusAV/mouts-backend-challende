using AutoMapper;
using FluentValidation;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;

namespace Ambev.DeveloperEvaluation.Application.Sales.UpdateSale;

/// <summary>
/// Handler de atualização de venda.
/// DIP: todas as dependências são abstrações injetadas.
/// </summary>
public class UpdateSaleHandler : IRequestHandler<UpdateSaleCommand, UpdateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IValidator<UpdateSaleCommand> _validator;
    private readonly IDiscountStrategy _discountStrategy;

    public UpdateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IMediator mediator,
        IValidator<UpdateSaleCommand> validator,
        IDiscountStrategy discountStrategy)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _mediator = mediator;
        _validator = validator;
        _discountStrategy = discountStrategy;
    }

    public async Task<UpdateSaleResult> Handle(UpdateSaleCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = await _saleRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Venda {command.Id} não encontrada.");

        if (sale.IsCancelled)
            throw new InvalidOperationException("Não é possível modificar uma venda cancelada.");

        // Usa método de domínio para manter encapsulamento (não muta propriedades diretamente)
        sale.Update(command.CustomerId, command.CustomerName, command.BranchId, command.BranchName, command.SaleDate);

        var newItems = command.Items.Select(i => _mapper.Map<SaleItem>(i)).ToList();
        sale.SetItems(newItems, _discountStrategy);

        var updated = await _saleRepository.UpdateAsync(sale, cancellationToken);

        await _mediator.Publish(
            new SaleModifiedEvent(updated.Id, updated.SaleNumber, updated.TotalAmount),
            cancellationToken);

        return _mapper.Map<UpdateSaleResult>(updated);
    }
}
