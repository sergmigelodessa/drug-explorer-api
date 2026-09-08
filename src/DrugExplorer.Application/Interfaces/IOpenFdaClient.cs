using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IOpenFdaClient
{
    Task<IReadOnlyList<DrugCandidate>> SearchAsync(string query);
}
