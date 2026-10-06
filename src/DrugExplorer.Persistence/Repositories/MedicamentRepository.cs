using Microsoft.EntityFrameworkCore;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Persistence.Repositories;

public class MedicamentRepo : IMedicamentRepository
{
    private readonly DrugExplorerDbContext _context;

    public MedicamentRepo(DrugExplorerDbContext context)
    {
        _context = context;
    }

    public async Task<Drug> InsertAsync(
        Drug drug,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(drug);

        _context.Drugs.Add(drug);
        await _context.SaveChangesAsync(cancellationToken);

        return drug;
    }

    public async Task<Drug?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Drugs
            .AsNoTracking()
            .FirstOrDefaultAsync(drug => drug.Id == id, cancellationToken);
    }

    public async Task<HashSet<string>> GetExistingOpenFdaIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = await _context.Drugs
            .AsNoTracking()
            .Where(m => m.OpenFdaId != null)
            .Select(m => m.OpenFdaId!)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(ids);
    }

    public async Task<Dictionary<string, int>> GetGenericNameCountsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Drugs
            .AsNoTracking()
            .Where(drug => drug.GenericName != null && drug.GenericName != "")
            .GroupBy(drug => drug.GenericName!.Trim().ToUpper())
            .Select(group => new { GenericName = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.GenericName, item => item.Count, cancellationToken);
    }

    public async Task<int> AddRangeAsync(
        IEnumerable<Drug> drugs,
        CancellationToken cancellationToken = default)
    {
        var toInsert = drugs.ToList();
        if (toInsert.Count == 0)
        {
            return 0;
        }

        _context.Drugs.AddRange(toInsert);
        await _context.SaveChangesAsync(cancellationToken);

        return toInsert.Count;
    }
}