namespace DrugExplorer.Domain.Models;

public class DrugCandidate
{
    public string BrandName { get; init; } = string.Empty;

    public string GenericName { get; init; } = string.Empty;

    public string Manufacturer { get; init; } = string.Empty;

    public string? DosageForm { get; init; }

    public string? Route { get; init; }

    public string? Purpose { get; init; }

    public string? Warnings { get; init; }

    public string? ActiveIngredient { get; init; }

    public string? DoNotUse { get; init; }

    public string? AskDoctor { get; init; }

    public string? AskDoctorOrPharmacist { get; init; }

    public string? WhenUsing { get; init; }

    public string? StopUse { get; init; }

    public string? PregnancyOrBreastFeeding { get; init; }

    public string? KeepOutOfReachOfChildren { get; init; }

    public string? DosageAndAdministration { get; init; }

    public string? InactiveIngredient { get; init; }

    public string? ProductType { get; init; }

    public string? ApplicationNumber { get; init; }
}
