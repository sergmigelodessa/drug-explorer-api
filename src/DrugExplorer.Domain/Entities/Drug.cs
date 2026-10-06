namespace DrugExplorer.Domain.Entities;

public class Drug
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string? OpenFdaId { get; set; }
    public string? SetId { get; set; }
    public string? EffectiveTime { get; set; }
    public string? Version { get; set; }
    public string? BrandName { get; set; }
    public string? GenericName { get; set; }
    public string? ManufacturerName { get; set; }
    public string? DosageForm { get; set; }
    public string? Route { get; set; }
    public string? ProductNdc { get; set; }
    public string? PackageNdc { get; set; }
    public string? ProductType { get; set; }
    public string? SubstanceName { get; set; }
    public string? Rxcui { get; set; }
    public string? SplId { get; set; }
    public string? SplSetId { get; set; }
    public bool? IsOriginalPackager { get; set; }
    public string? PharmClassMoa { get; set; }
    public string? PharmClassCs { get; set; }
    public string? PharmClassEpc { get; set; }
    public string? Unii { get; set; }
    public string? ActiveIngredient { get; set; }
    public string? Purpose { get; set; }
    public string? IndicationsAndUsage { get; set; }
    public string? Warnings { get; set; }
    public string? DoNotUse { get; set; }
    public string? AskDoctor { get; set; }
    public string? AskDoctorOrPharmacist { get; set; }
    public string? WhenUsing { get; set; }
    public string? StopUse { get; set; }
    public string? PregnancyOrBreastFeeding { get; set; }
    public string? KeepOutOfReachOfChildren { get; set; }
    public string? DosageAndAdministration { get; set; }
    public string? DosageAndAdministrationTable { get; set; }
    public string? InactiveIngredient { get; set; }
    public string? SplProductDataElements { get; set; }
    public string? SplUnclassifiedSection { get; set; }
    public string? PackageLabelPrincipalDisplayPanel { get; set; }
    public string? RecentMajorChanges { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}