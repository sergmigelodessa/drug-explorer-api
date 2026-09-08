using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IDrugSearchService
{
    Task<DrugSearchResult> SearchAsync(string query);
}
