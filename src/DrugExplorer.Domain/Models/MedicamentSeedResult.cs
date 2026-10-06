namespace DrugExplorer.Domain.Models;

public class MedicamentSeedResult
{
    public int TotalInserted { get; set; }

    public int TotalSkippedDuplicates { get; set; }

    public int TotalSkippedByLimit { get; set; }

    public int QueriesUsed { get; set; }

    public int RequestsMade { get; set; }

    public bool ReachedTarget { get; set; }
}
