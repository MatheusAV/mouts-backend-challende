namespace Ambev.DeveloperEvaluation.Domain.Services;

/// <summary>
/// Contrato para geração de números de venda únicos.
/// SRP: responsabilidade de gerar o número isolada do handler.
/// DIP: handler depende desta abstração, não da implementação concreta.
/// </summary>
public interface ISaleNumberGenerator
{
    string Generate();
}
