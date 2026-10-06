using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IOpenFdaClient
{
    Task<IReadOnlyList<DrugCandidate>> SearchAsync(string query);

    // Raw drug/label lookup (mapped straight to Drug) used for bulk seeding, with paging support.
    Task<MedicamentPage> SearchMedicamentsAsync(string query, int limit, int skip, CancellationToken cancellationToken = default);
}
