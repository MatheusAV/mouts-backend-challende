using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly DefaultContext _context;

    public SaleRepository(DefaultContext context) => _context = context;

    public async Task<Sale> CreateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        await _context.Sales.AddAsync(sale, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<Sale?> GetBySaleNumberAsync(string saleNumber, CancellationToken cancellationToken = default)
        => await _context.Sales
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.SaleNumber == saleNumber, cancellationToken);

    public async Task<IEnumerable<Sale>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default)
        => await _context.Sales
            .Include(s => s.Items)
            .OrderByDescending(s => s.SaleDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public async Task<Sale> UpdateAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        // Remove DB items that are no longer in the sale (replaced or cleared)
        var existingItemIds = await _context.SaleItems
            .Where(i => i.SaleId == sale.Id)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);

        var currentItemIds = sale.Items.Select(i => i.Id).ToHashSet();
        var toRemove = existingItemIds.Where(id => !currentItemIds.Contains(id)).ToList();

        if (toRemove.Count > 0)
        {
            var itemsToRemove = await _context.SaleItems
                .Where(i => toRemove.Contains(i.Id))
                .ToListAsync(cancellationToken);
            _context.SaleItems.RemoveRange(itemsToRemove);
        }

        _context.Entry(sale).State = EntityState.Modified;

        foreach (var item in sale.Items)
        {
            var state = existingItemIds.Contains(item.Id)
                ? EntityState.Modified
                : EntityState.Added;
            _context.Entry(item).State = state;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return sale;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await GetByIdAsync(id, cancellationToken);
        if (sale is null) return false;

        _context.Sales.Remove(sale);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
