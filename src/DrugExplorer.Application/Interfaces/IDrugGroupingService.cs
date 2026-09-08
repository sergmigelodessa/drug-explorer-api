using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IDrugGroupingService
{
    List<DrugGroup> Group(IReadOnlyList<DrugCandidate> candidates);
}
