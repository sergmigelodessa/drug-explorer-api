namespace DrugExplorer.Api.Dto;

public class DrugVariantDto
{
    public string Name { get; set; } = string.Empty;

    public string? Manufacturer { get; set; }

    public string? DosageForm { get; set; }

    public string? Route { get; set; }

    public string? Purpose { get; set; }

    public string? Warnings { get; set; }

    public string? ActiveIngredient { get; set; }

    public string? DoNotUse { get; set; }

    public string? AskDoctor { get; set; }

    public string? AskDoctorOrPharmacist { get; set; }

    public string? WhenUsing { get; set; }

    public string? StopUse { get; set; }

    public string? PregnancyOrBreastFeeding { get; set; }

    public string? KeepOutOfReachOfChildren { get; set; }

    public string? DosageAndAdministration { get; set; }

    public string? InactiveIngredient { get; set; }

    public string? ProductType { get; set; }

    public string? ApplicationNumber { get; set; }
}
