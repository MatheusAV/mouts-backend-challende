namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Implementação padrão das regras de desconto por quantidade:
///   - Acima de 20 itens  → DomainException (limite máximo)
///   - 10 a 20 itens      → 20% de desconto
///   - 4  a  9 itens      → 10% de desconto
///   - Menos de 4 itens   → sem desconto
/// </summary>
public sealed class QuantityDiscountStrategy : IDiscountStrategy
{
    public const int MaxQuantity = 20;

    public decimal Calculate(int quantity)
    {
        if (quantity > MaxQuantity)
            throw new DomainException($"Não é possível vender mais de {MaxQuantity} itens idênticos.");

        return quantity switch
        {
            >= 10 => 0.20m,
            >= 4  => 0.10m,
            _     => 0.00m
        };
    }
}
