using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Services;

public class DrugGroupingService : IDrugGroupingService
{
    public List<DrugGroup> Group(IReadOnlyList<DrugCandidate> variants)
    {
        if (variants == null || variants.Count == 0)
            return new List<DrugGroup>();

        var groups = variants
            .GroupBy(v => GetGroupKey(v))
            .Select(g => new DrugGroup
            {
                GroupName = g.Key,
                GenericName = g.Select(v => v.GenericName)
                    .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)),
                TotalVariants = g.Count(),
                Variants = g.Select(v => new DrugVariant
                {
                    Name = v.BrandName,
                    DosageForm = v.DosageForm,
                    Manufacturer = v.Manufacturer,
                    Route = v.Route,
                    Strength = null,
                    Purpose = v.Purpose,
                    Warnings = v.Warnings,
                    ActiveIngredient = v.ActiveIngredient,
                    DoNotUse = v.DoNotUse,
                    AskDoctor = v.AskDoctor,
                    AskDoctorOrPharmacist = v.AskDoctorOrPharmacist,
                    WhenUsing = v.WhenUsing,
                    StopUse = v.StopUse,
                    PregnancyOrBreastFeeding = v.PregnancyOrBreastFeeding,
                    KeepOutOfReachOfChildren = v.KeepOutOfReachOfChildren,
                    DosageAndAdministration = v.DosageAndAdministration,
                    InactiveIngredient = v.InactiveIngredient,
                    ProductType = v.ProductType,
                    ApplicationNumber = v.ApplicationNumber
                }).ToList(),
                Score = CalculateScore(g.ToList())
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        return groups;
    }

    private string GetGroupKey(DrugCandidate v)
    {
        if (!string.IsNullOrWhiteSpace(v.BrandName))
            return v.BrandName.Trim().ToLowerInvariant();

        if (!string.IsNullOrWhiteSpace(v.GenericName))
            return v.GenericName.Trim().ToLowerInvariant();

        return "unknown";
    }

    private double CalculateScore(List<DrugCandidate> group)
    {
        var score = 0.0;

        score += group.Count * 10;

        if (group.Any(x => !string.IsNullOrWhiteSpace(x.Manufacturer)))
            score += 5;

        if (group.Any(x => !string.IsNullOrWhiteSpace(x.DosageForm)))
            score += 5;

        if (group.Any(x => !string.IsNullOrWhiteSpace(x.Purpose)))
            score += 3;

        return score;
    }
}