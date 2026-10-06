using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Domain.Models;

// One page of raw OpenFDA drug/label results already mapped to Drug entities.
public class MedicamentPage
{
    public IReadOnlyList<Drug> Items { get; init; } = Array.Empty<Drug>();

    public int Total { get; init; }
}
