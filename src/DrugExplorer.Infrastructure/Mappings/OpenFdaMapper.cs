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
}
