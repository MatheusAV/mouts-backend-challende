using AutoMapper;
using FluentValidation;
using MediatR;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;

namespace Ambev.DeveloperEvaluation.Application.Sales.CreateSale;

/// <summary>
/// Handler de criação de venda.
/// DIP: todas as dependências são abstrações injetadas.
/// SRP: responsabilidade única de orquestrar a criação de uma venda.
/// </summary>
public class CreateSaleHandler : IRequestHandler<CreateSaleCommand, CreateSaleResult>
{
    private readonly ISaleRepository _saleRepository;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;
    private readonly IValidator<CreateSaleCommand> _validator;
    private readonly ISaleNumberGenerator _saleNumberGenerator;
    private readonly IDiscountStrategy _discountStrategy;

    public CreateSaleHandler(
        ISaleRepository saleRepository,
        IMapper mapper,
        IMediator mediator,
        IValidator<CreateSaleCommand> validator,
        ISaleNumberGenerator saleNumberGenerator,
        IDiscountStrategy discountStrategy)
    {
        _saleRepository = saleRepository;
        _mapper = mapper;
        _mediator = mediator;
        _validator = validator;
        _saleNumberGenerator = saleNumberGenerator;
        _discountStrategy = discountStrategy;
    }

    public async Task<CreateSaleResult> Handle(CreateSaleCommand command, CancellationToken cancellationToken)
    {
        var validationResult = await _validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var sale = _mapper.Map<Sale>(command);
        sale.Id = Guid.NewGuid();
        sale.SaleNumber = _saleNumberGenerator.Generate();

        var items = command.Items.Select(i => _mapper.Map<SaleItem>(i)).ToList();
        sale.SetItems(items, _discountStrategy);

        var created = await _saleRepository.CreateAsync(sale, cancellationToken);

        await _mediator.Publish(
            new SaleCreatedEvent(created.Id, created.SaleNumber, created.CustomerId, created.TotalAmount),
            cancellationToken);

        return _mapper.Map<CreateSaleResult>(created);
    }
}
