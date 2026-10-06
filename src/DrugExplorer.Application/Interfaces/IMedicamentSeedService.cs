using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IMedicamentSeedService
{
    // Populates the Drugs table from OpenFDA using a broad set of built-in search terms.
    Task<MedicamentSeedResult> SeedAsync(int targetCount, CancellationToken cancellationToken = default);
}
