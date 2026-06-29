namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Gera número de venda no formato SALE-YYYYMMDD-XXXXXXXX.
/// </summary>
public sealed class SaleNumberGenerator : ISaleNumberGenerator
{
    public string Generate()
        => $"SALE-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
}
