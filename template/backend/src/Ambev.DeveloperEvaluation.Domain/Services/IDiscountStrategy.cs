namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Contrato para cálculo de desconto baseado em quantidade.
/// OCP: novas regras de desconto podem ser adicionadas sem modificar o código existente.
/// </summary>
public interface IDiscountStrategy
{
    /// <summary>Calcula o percentual de desconto (0.00, 0.10, 0.20) para a quantidade informada.</summary>
    /// <exception cref="DomainException">Se a quantidade exceder o limite máximo permitido.</exception>
    decimal Calculate(int quantity);
}
