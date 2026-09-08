namespace DrugExplorer.Domain.Models;

public class DrugGroup
{
    public string GroupName { get; set; } = string.Empty;

    public string? GenericName { get; set; }

    public int TotalVariants { get; set; }

    public double Score { get; set; }

    public List<DrugVariant> Variants { get; set; } = new();
}
