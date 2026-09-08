namespace DrugExplorer.Application.Interfaces;

public interface ISemanticSearchService
{
    Task<List<Domain.Models.SemanticDrugResult>> SearchAsync(
        string query,
        int topK = 10,
        CancellationToken cancellationToken = default);
}
