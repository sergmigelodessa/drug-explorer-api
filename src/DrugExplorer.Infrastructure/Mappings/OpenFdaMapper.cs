using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Models;
using DrugExplorer.Infrastructure.Models;

namespace DrugExplorer.Infrastructure.Mappings;

public class OpenFdaMapper : IOpenFdaMapper
{
    public DrugCandidate Map(OpenFdaDrugDto dto)
    {
        return new DrugCandidate
        {
            BrandName = dto.OpenFda.BrandName?.FirstOrDefault() ?? string.Empty,
            GenericName = dto.OpenFda.GenericName?.FirstOrDefault() ?? string.Empty,
            Manufacturer = dto.OpenFda.Manufacturer?.FirstOrDefault() ?? string.Empty,
            DosageForm = dto.OpenFda.DosageForm?.FirstOrDefault(),
            Route = dto.OpenFda.Route?.FirstOrDefault(),
            Purpose = dto.Purpose?.FirstOrDefault() ?? dto.Indications?.FirstOrDefault(),
            Warnings = dto.Warnings?.FirstOrDefault(),
            ActiveIngredient = dto.ActiveIngredient?.FirstOrDefault(),
            DoNotUse = dto.DoNotUse?.FirstOrDefault(),
            AskDoctor = dto.AskDoctor?.FirstOrDefault(),
            AskDoctorOrPharmacist = dto.AskDoctorOrPharmacist?.FirstOrDefault(),
            WhenUsing = dto.WhenUsing?.FirstOrDefault(),
            StopUse = dto.StopUse?.FirstOrDefault(),
            PregnancyOrBreastFeeding = dto.PregnancyOrBreastFeeding?.FirstOrDefault(),
            KeepOutOfReachOfChildren = dto.KeepOutOfReachOfChildren?.FirstOrDefault(),
            DosageAndAdministration = dto.DosageAndAdministration?.FirstOrDefault(),
            InactiveIngredient = dto.InactiveIngredient?.FirstOrDefault(),
            ProductType = dto.OpenFda.ProductType?.FirstOrDefault(),
            ApplicationNumber = dto.OpenFda.ApplicationNumber?.FirstOrDefault()
        };
    }

    public Drug MapToDrug(OpenFdaDrugDto dto)
    {
        return new Drug
        {
            OpenFdaId = dto.Id,
            SetId = dto.SetId,
            EffectiveTime = dto.EffectiveTime,
            Version = dto.Version,
            BrandName = dto.OpenFda.BrandName?.FirstOrDefault(),
            GenericName = dto.OpenFda.GenericName?.FirstOrDefault(),
            ManufacturerName = dto.OpenFda.Manufacturer?.FirstOrDefault(),
            DosageForm = dto.OpenFda.DosageForm?.FirstOrDefault(),
            Route = dto.OpenFda.Route?.FirstOrDefault(),
            ProductNdc = dto.OpenFda.ProductNdc?.FirstOrDefault(),
            PackageNdc = dto.OpenFda.PackageNdc?.FirstOrDefault(),
            ProductType = dto.OpenFda.ProductType?.FirstOrDefault(),
            SubstanceName = dto.OpenFda.SubstanceName?.FirstOrDefault(),
            Rxcui = dto.OpenFda.Rxcui?.FirstOrDefault(),
            SplId = dto.OpenFda.SplId?.FirstOrDefault(),
            SplSetId = dto.OpenFda.SplSetId?.FirstOrDefault(),
            IsOriginalPackager = dto.OpenFda.IsOriginalPackager?.FirstOrDefault(),
            PharmClassMoa = dto.OpenFda.PharmClassMoa?.FirstOrDefault(),
            PharmClassCs = dto.OpenFda.PharmClassCs?.FirstOrDefault(),
            PharmClassEpc = dto.OpenFda.PharmClassEpc?.FirstOrDefault(),
            Unii = dto.OpenFda.Unii?.FirstOrDefault(),
            ActiveIngredient = dto.ActiveIngredient?.FirstOrDefault(),
            Purpose = dto.Purpose?.FirstOrDefault() ?? dto.Indications?.FirstOrDefault(),
            IndicationsAndUsage = dto.Indications?.FirstOrDefault(),
            Warnings = dto.Warnings?.FirstOrDefault(),
            DoNotUse = dto.DoNotUse?.FirstOrDefault(),
            AskDoctor = dto.AskDoctor?.FirstOrDefault(),
            AskDoctorOrPharmacist = dto.AskDoctorOrPharmacist?.FirstOrDefault(),
            WhenUsing = dto.WhenUsing?.FirstOrDefault(),
            StopUse = dto.StopUse?.FirstOrDefault(),
            PregnancyOrBreastFeeding = dto.PregnancyOrBreastFeeding?.FirstOrDefault(),
            KeepOutOfReachOfChildren = dto.KeepOutOfReachOfChildren?.FirstOrDefault(),
            DosageAndAdministration = dto.DosageAndAdministration?.FirstOrDefault(),
            DosageAndAdministrationTable = dto.DosageAndAdministrationTable?.FirstOrDefault(),
            InactiveIngredient = dto.InactiveIngredient?.FirstOrDefault(),
            SplProductDataElements = dto.SplProductDataElements?.FirstOrDefault(),
            SplUnclassifiedSection = dto.SplUnclassifiedSection?.FirstOrDefault(),
            PackageLabelPrincipalDisplayPanel = dto.PackageLabelPrincipalDisplayPanel?.FirstOrDefault(),
            RecentMajorChanges = dto.RecentMajorChanges?.FirstOrDefault()
        };
    }
}
