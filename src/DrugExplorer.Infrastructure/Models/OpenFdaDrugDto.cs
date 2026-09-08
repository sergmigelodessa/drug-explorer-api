using System.Text.Json.Serialization;

namespace DrugExplorer.Infrastructure.Models;

public class OpenFdaDrugDto
{
    [JsonPropertyName("openfda")]
    public OpenFdaInfo OpenFda { get; set; } = new();

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("set_id")]
    public string? SetId { get; set; }

    [JsonPropertyName("effective_time")]
    public string? EffectiveTime { get; set; }

    [JsonPropertyName("version")]
    public string? Version { get; set; }

    [JsonPropertyName("active_ingredient")]
    public List<string>? ActiveIngredient { get; set; }

    [JsonPropertyName("purpose")]
    public List<string>? Purpose { get; set; }

    [JsonPropertyName("indications_and_usage")]
    public List<string>? Indications { get; set; }

    [JsonPropertyName("warnings")]
    public List<string>? Warnings { get; set; }

    [JsonPropertyName("do_not_use")]
    public List<string>? DoNotUse { get; set; }

    [JsonPropertyName("ask_doctor")]
    public List<string>? AskDoctor { get; set; }

    [JsonPropertyName("ask_doctor_or_pharmacist")]
    public List<string>? AskDoctorOrPharmacist { get; set; }

    [JsonPropertyName("when_using")]
    public List<string>? WhenUsing { get; set; }

    [JsonPropertyName("stop_use")]
    public List<string>? StopUse { get; set; }

    [JsonPropertyName("pregnancy_or_breast_feeding")]
    public List<string>? PregnancyOrBreastFeeding { get; set; }

    [JsonPropertyName("keep_out_of_reach_of_children")]
    public List<string>? KeepOutOfReachOfChildren { get; set; }

    [JsonPropertyName("dosage_and_administration")]
    public List<string>? DosageAndAdministration { get; set; }

    [JsonPropertyName("dosage_and_administration_table")]
    public List<string>? DosageAndAdministrationTable { get; set; }

    [JsonPropertyName("inactive_ingredient")]
    public List<string>? InactiveIngredient { get; set; }

    [JsonPropertyName("spl_product_data_elements")]
    public List<string>? SplProductDataElements { get; set; }

    [JsonPropertyName("spl_unclassified_section")]
    public List<string>? SplUnclassifiedSection { get; set; }

    [JsonPropertyName("package_label_principal_display_panel")]
    public List<string>? PackageLabelPrincipalDisplayPanel { get; set; }

    [JsonPropertyName("recent_major_changes")]
    public List<string>? RecentMajorChanges { get; set; }
}

public class OpenFdaInfo
{
    [JsonPropertyName("application_number")]
    public List<string>? ApplicationNumber { get; set; }

    [JsonPropertyName("brand_name")]
    public List<string>? BrandName { get; set; }

    [JsonPropertyName("generic_name")]
    public List<string>? GenericName { get; set; }

    [JsonPropertyName("manufacturer_name")]
    public List<string>? Manufacturer { get; set; }

    [JsonPropertyName("route")]
    public List<string>? Route { get; set; }

    [JsonPropertyName("dosage_form")]
    public List<string>? DosageForm { get; set; }

    [JsonPropertyName("product_ndc")]
    public List<string>? ProductNdc { get; set; }

    [JsonPropertyName("package_ndc")]
    public List<string>? PackageNdc { get; set; }

    [JsonPropertyName("product_type")]
    public List<string>? ProductType { get; set; }

    [JsonPropertyName("substance_name")]
    public List<string>? SubstanceName { get; set; }

    [JsonPropertyName("rxcui")]
    public List<string>? Rxcui { get; set; }

    [JsonPropertyName("spl_id")]
    public List<string>? SplId { get; set; }

    [JsonPropertyName("spl_set_id")]
    public List<string>? SplSetId { get; set; }

    [JsonPropertyName("is_original_packager")]
    public List<bool>? IsOriginalPackager { get; set; }

    [JsonPropertyName("nui")]
    public List<string>? Nui { get; set; }

    [JsonPropertyName("pharm_class_moa")]
    public List<string>? PharmClassMoa { get; set; }

    [JsonPropertyName("pharm_class_cs")]
    public List<string>? PharmClassCs { get; set; }

    [JsonPropertyName("pharm_class_epc")]
    public List<string>? PharmClassEpc { get; set; }

    [JsonPropertyName("unii")]
    public List<string>? Unii { get; set; }
}
