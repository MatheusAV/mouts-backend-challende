using AutoMapper;
using FluentAssertions;
using FluentValidation;
using MediatR;
using NSubstitute;
using Ambev.DeveloperEvaluation.Application.Sales.CreateSale;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Ambev.DeveloperEvaluation.Domain.Services;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Application.Sales;

public class CreateSaleHandlerTests
{
    private readonly ISaleRepository _saleRepository = Substitute.For<ISaleRepository>();
    private readonly IMapper _mapper;
    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly IValidator<CreateSaleCommand> _validator;
    private readonly ISaleNumberGenerator _saleNumberGenerator;
    private readonly IDiscountStrategy _discountStrategy;
    private readonly CreateSaleHandler _handler;

    public CreateSaleHandlerTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CreateSaleProfile>());
        _mapper = config.CreateMapper();

        _validator = new CreateSaleCommandValidator();
        _saleNumberGenerator = new SaleNumberGenerator();
        _discountStrategy = new QuantityDiscountStrategy();

        _handler = new CreateSaleHandler(
            _saleRepository, _mapper, _mediator,
            _validator, _saleNumberGenerator, _discountStrategy);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSaleResult()
    {
        // Arrange
        var command = BuildValidCommand(quantity: 5);
        _saleRepository.CreateAsync(Arg.Any<Sale>(), Arg.Any<CancellationToken>())
            .Returns(ci => ci.Arg<Sale>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SaleNumber.Should().StartWith("SALE-");
        result.Items.Should().HaveCount(1);
        result.Items[0].Discount.Should().Be(0.10m);   // 5 items → 10%
    }

    [Fact]
    public async Task Handle_21Items_ThrowsDomainException()
    {
        var command = BuildValidCommand(quantity: 21);

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Handle_EmptyItems_ThrowsValidationException()
    {
        var command = BuildValidCommand(quantity: 1);
        command.Items.Clear();

        var act = async () => await _handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
    }

    private static CreateSaleCommand BuildValidCommand(int quantity) => new()
    {
        CustomerId = Guid.NewGuid(),
        CustomerName = "ACME Corp",
        BranchId = Guid.NewGuid(),
        BranchName = "Main Branch",
        SaleDate = DateTime.UtcNow,
        Items = new List<CreateSaleItemCommand>
        {
            new()
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Widget",
                Quantity = quantity,
                UnitPrice = 50m
            }
        }
    };
}
