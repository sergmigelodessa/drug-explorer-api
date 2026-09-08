namespace DrugExplorer.Api.Dto;

public class DrugGroupDto
{
    public string GroupName { get; set; } = string.Empty;

    public string? GenericName { get; set; }

    public int TotalVariants { get; set; }

    public double Score { get; set; }

    public List<DrugVariantDto> Variants { get; set; } = new();
}
