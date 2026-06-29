using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Services;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Representa um item de venda com suas regras de desconto por quantidade.
/// </summary>
public class SaleItem : BaseEntity
{
    /// <summary>Chave estrangeira para a venda pai.</summary>
    public Guid SaleId { get; set; }

    // --- Identidade Externa: Produto (desnormalizado) ---
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>Percentual de desconto aplicado (0, 0.10 ou 0.20).</summary>
    public decimal Discount { get; private set; }

    /// <summary>Total do item após desconto: Quantidade * PreçoUnit * (1 - Desconto).</summary>
    public decimal TotalAmount { get; private set; }

    public bool IsCancelled { get; private set; }

    // Navegação EF Core
    public Sale? Sale { get; set; }

    /// <summary>
    /// Aplica a estratégia de desconto e recalcula o TotalAmount.
    /// OCP: o comportamento do desconto pode ser trocado sem alterar esta classe.
    /// </summary>
    public void ApplyDiscount(IDiscountStrategy discountStrategy)
    {
        Discount = discountStrategy.Calculate(Quantity);
        TotalAmount = Quantity * UnitPrice * (1 - Discount);
    }

    /// <summary>Cancela este item.</summary>
    public void Cancel() => IsCancelled = true;
}
