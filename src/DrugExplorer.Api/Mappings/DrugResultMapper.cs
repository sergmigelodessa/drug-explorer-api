using DrugExplorer.Api.Dto;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Api.Mappings;

public class DrugResultMapper
{
    public DrugSearchResponseDto MapSearchResult(DrugSearchResult domainResult)
    {
        return new DrugSearchResponseDto
        {
            NormalizedQuery = domainResult.NormalizedQuery,
            Groups = domainResult.Groups.Select(MapGroup).ToList()
        };
    }

    public SemanticSearchResponseDto MapSemanticResult(string query, List<SemanticDrugResult> results)
    {
        return new SemanticSearchResponseDto
        {
            Query = query,
            Results = results.Select(r => new SemanticDrugResultDto
            {
                BrandName = r.BrandName,
                GenericName = r.GenericName,
                ChunkType = r.ChunkType.ToString(),
                MatchedText = r.MatchedText,
                Score = Math.Round(r.Score, 4)
            }).ToList()
        };
    }

    public AskResponseDto MapRagAnswer(RagAnswer answer)
    {
        return new AskResponseDto
        {
            Answer = answer.Answer,
            Grounded = answer.Grounded,
            Sources = answer.Sources.Select(s => new RagSourceDto
            {
                BrandName = s.BrandName,
                GenericName = s.GenericName,
                ChunkType = s.ChunkType,
                ChunkText = s.ChunkText,
                Similarity = Math.Round(s.Similarity, 4)
            }).ToList()
        };
    }

    private DrugGroupDto MapGroup(DrugGroup group)
    {
        return new DrugGroupDto
        {
            GroupName = group.GroupName,
            GenericName = group.GenericName,
            TotalVariants = group.TotalVariants,
            Score = group.Score,
            Variants = group.Variants.Select(MapVariant).ToList()
        };
    }

    private DrugVariantDto MapVariant(DrugVariant variant)
    {
        return new DrugVariantDto
        {
            Name = variant.Name,
            Manufacturer = variant.Manufacturer,
            DosageForm = variant.DosageForm,
            Route = variant.Route,
            Purpose = variant.Purpose,
            Warnings = variant.Warnings,
            ActiveIngredient = variant.ActiveIngredient,
            DoNotUse = variant.DoNotUse,
            AskDoctor = variant.AskDoctor,
            AskDoctorOrPharmacist = variant.AskDoctorOrPharmacist,
            WhenUsing = variant.WhenUsing,
            StopUse = variant.StopUse,
            PregnancyOrBreastFeeding = variant.PregnancyOrBreastFeeding,
            KeepOutOfReachOfChildren = variant.KeepOutOfReachOfChildren,
            DosageAndAdministration = variant.DosageAndAdministration,
            InactiveIngredient = variant.InactiveIngredient,
            ProductType = variant.ProductType,
            ApplicationNumber = variant.ApplicationNumber
        };
    }
}
