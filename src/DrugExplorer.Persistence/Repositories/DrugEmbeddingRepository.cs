using Microsoft.EntityFrameworkCore;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Enums;

namespace DrugExplorer.Persistence.Repositories;

public class DrugEmbeddingRepository : IDrugEmbeddingRepository
{
    private readonly DrugExplorerDbContext _context;

    public DrugEmbeddingRepository(DrugExplorerDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string drugKey, DrugChunkType chunkType, CancellationToken cancellationToken = default)
    {
        return await _context.DrugEmbeddings
            .AsNoTracking()
            .AnyAsync(e => e.DrugKey == drugKey && e.ChunkType == chunkType, cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<DrugEmbedding> embeddings, CancellationToken cancellationToken = default)
    {
        _context.DrugEmbeddings.AddRange(embeddings);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<DrugEmbedding>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DrugEmbeddings
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.DrugEmbeddings.CountAsync(cancellationToken);
    }
}
