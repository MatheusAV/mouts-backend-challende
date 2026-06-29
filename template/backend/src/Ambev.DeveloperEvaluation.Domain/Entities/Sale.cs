using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Services;

namespace Ambev.DeveloperEvaluation.Domain.Entities;

/// <summary>
/// Aggregate root de uma transação de venda.
/// Usa o padrão External Identities para referenciar Cliente e Filial de outros domínios.
/// </summary>
public class Sale : BaseEntity
{
    /// <summary>Número legível da venda (ex.: "SALE-20241201-ABC12345").</summary>
    public string SaleNumber { get; set; } = string.Empty;

    public DateTime SaleDate { get; set; }

    // --- Identidade Externa: Cliente (desnormalizado) ---
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;

    // --- Identidade Externa: Filial (desnormalizado) ---
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;

    /// <summary>Soma dos TotalAmounts dos itens não cancelados.</summary>
    public decimal TotalAmount { get; private set; }

    public bool IsCancelled { get; private set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    private readonly List<SaleItem> _items = new();
    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    public Sale()
    {
        CreatedAt = DateTime.UtcNow;
        SaleDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Atualiza os dados da venda via método de domínio (encapsulamento).
    /// </summary>
    public void Update(Guid customerId, string customerName, Guid branchId, string branchName, DateTime saleDate)
    {
        CustomerId = customerId;
        CustomerName = customerName;
        BranchId = branchId;
        BranchName = branchName;
        SaleDate = saleDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Adiciona um item e recalcula o total.</summary>
    public void AddItem(SaleItem item, IDiscountStrategy discountStrategy)
    {
        if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
        item.SaleId = Id;
        item.ApplyDiscount(discountStrategy);
        _items.Add(item);
        RecalculateTotal();
    }

    /// <summary>Substitui todos os itens. Itens novos recebem ID gerado.</summary>
    public void SetItems(IEnumerable<SaleItem> items, IDiscountStrategy discountStrategy)
    {
        _items.Clear();
        foreach (var item in items)
        {
            if (item.Id == Guid.Empty) item.Id = Guid.NewGuid();
            item.SaleId = Id;
            item.ApplyDiscount(discountStrategy);
            _items.Add(item);
        }
        RecalculateTotal();
    }

    /// <summary>Cancela a venda inteira.</summary>
    public void Cancel()
    {
        IsCancelled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Cancela um item específico e recalcula o total.</summary>
    public void CancelItem(Guid itemId)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new DomainException($"Item {itemId} não encontrado na venda.");

        item.Cancel();
        UpdatedAt = DateTime.UtcNow;
        RecalculateTotal();
    }

    private void RecalculateTotal()
        => TotalAmount = _items.Where(i => !i.IsCancelled).Sum(i => i.TotalAmount);
}
