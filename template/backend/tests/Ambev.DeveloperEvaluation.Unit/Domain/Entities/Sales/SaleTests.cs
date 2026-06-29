using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Ambev.DeveloperEvaluation.Unit.Domain.Entities.Sales;

/// <summary>
/// Testes de unidade para a entidade Sale e SaleItem.
/// Cobrem todas as regras de negócio de desconto por quantidade.
/// </summary>
public class SaleTests
{
    private static readonly IDiscountStrategy Strategy = new QuantityDiscountStrategy();

    // ── Regras de desconto em SaleItem ────────────────────────────────────────

    [Theory]
    [InlineData(1, 0.00)]
    [InlineData(3, 0.00)]
    public void ApplyDiscount_MenosDe4Itens_SemDesconto(int quantity, decimal expectedDiscount)
    {
        var item = BuildItem(quantity);
        item.ApplyDiscount(Strategy);

        item.Discount.Should().Be(expectedDiscount);
        item.TotalAmount.Should().Be(quantity * item.UnitPrice);
    }

    [Theory]
    [InlineData(4,  0.10)]
    [InlineData(9,  0.10)]
    public void ApplyDiscount_4a9Itens_Desconto10Porcento(int quantity, decimal expectedDiscount)
    {
        var item = BuildItem(quantity);
        item.ApplyDiscount(Strategy);

        item.Discount.Should().Be(expectedDiscount);
        item.TotalAmount.Should().Be(quantity * item.UnitPrice * (1 - expectedDiscount));
    }

    [Theory]
    [InlineData(10, 0.20)]
    [InlineData(20, 0.20)]
    public void ApplyDiscount_10a20Itens_Desconto20Porcento(int quantity, decimal expectedDiscount)
    {
        var item = BuildItem(quantity);
        item.ApplyDiscount(Strategy);

        item.Discount.Should().Be(expectedDiscount);
        item.TotalAmount.Should().Be(quantity * item.UnitPrice * (1 - expectedDiscount));
    }

    [Fact]
    public void ApplyDiscount_MaisDe20Itens_LancaDomainException()
    {
        var item = BuildItem(21);
        var act = () => item.ApplyDiscount(Strategy);

        act.Should().Throw<DomainException>();
    }

    // ── Aggregate Sale ────────────────────────────────────────────────────────

    [Fact]
    public void AddItem_ItemValido_RecalculaTotal()
    {
        var sale = BuildSale();
        var item = BuildItem(5, 10m); // 5 itens × R$10 × (1 - 10%) = R$45

        sale.AddItem(item, Strategy);

        sale.TotalAmount.Should().Be(45m);
    }

    [Fact]
    public void CancelItem_ItemExistente_RemoveDoTotal()
    {
        var sale = BuildSale();
        var item1 = BuildItem(5, 10m);  // total = 45
        var item2 = BuildItem(2, 20m);  // total = 40 (sem desconto)

        sale.AddItem(item1, Strategy);
        sale.AddItem(item2, Strategy);

        sale.CancelItem(item1.Id);

        sale.TotalAmount.Should().Be(40m);
        item1.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void Cancel_VendaAtiva_DefineIsCancelledTrue()
    {
        var sale = BuildSale();
        sale.Cancel();

        sale.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void CancelItem_ItemNaoEncontrado_LancaDomainException()
    {
        var sale = BuildSale();
        var act = () => sale.CancelItem(Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_DadosValidos_AtualizaPropriedades()
    {
        var sale = BuildSale();
        var novoClienteId = Guid.NewGuid();
        var novaFilialId  = Guid.NewGuid();
        var novaData = DateTime.UtcNow.AddDays(1);

        sale.Update(novoClienteId, "Cliente Novo", novaFilialId, "Filial Nova", novaData);

        sale.CustomerId.Should().Be(novoClienteId);
        sale.CustomerName.Should().Be("Cliente Novo");
        sale.BranchId.Should().Be(novaFilialId);
        sale.BranchName.Should().Be("Filial Nova");
        sale.SaleDate.Should().Be(novaData);
        sale.UpdatedAt.Should().NotBeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SaleItem BuildItem(int quantity, decimal unitPrice = 10m) => new()
    {
        Id = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        ProductName = "Produto Teste",
        Quantity = quantity,
        UnitPrice = unitPrice
    };

    private static Sale BuildSale() => new()
    {
        Id = Guid.NewGuid(),
        SaleNumber = "SALE-TEST-001",
        CustomerId = Guid.NewGuid(),
        CustomerName = "Cliente Teste",
        BranchId = Guid.NewGuid(),
        BranchName = "Filial Teste"
    };
}
