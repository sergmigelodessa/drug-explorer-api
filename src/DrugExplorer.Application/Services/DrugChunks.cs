using System.Security.Cryptography;
using System.Text;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Enums;

namespace DrugExplorer.Application.Services;

public static class DrugChunks
{
    // Normalized "generic|brand" identity; empty when both names are missing.
    public static string BuildDrugKey(string? genericName, string? brandName)
    {
        var generic = genericName?.Trim().ToLowerInvariant() ?? string.Empty;
        var brand = brandName?.Trim().ToLowerInvariant() ?? string.Empty;

        return generic.Length == 0 && brand.Length == 0 ? string.Empty : $"{generic}|{brand}";
    }

    // Deterministic id so the same chunk always maps to the same vector point.
    public static Guid BuildPointId(string drugKey, DrugChunkType chunkType)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"{drugKey}#{chunkType}"));
        return new Guid(hash);
    }

    public static IEnumerable<(DrugChunkType ChunkType, string? Text)> Extract(Drug drug)
    {
        yield return (DrugChunkType.Purpose, drug.Purpose);
        yield return (DrugChunkType.Warnings, drug.Warnings);
        yield return (DrugChunkType.DoNotUse, drug.DoNotUse);
        yield return (DrugChunkType.AskDoctor, drug.AskDoctor);
        yield return (DrugChunkType.AskDoctorOrPharmacist, drug.AskDoctorOrPharmacist);
        yield return (DrugChunkType.PregnancyOrBreastFeeding, drug.PregnancyOrBreastFeeding);
        yield return (DrugChunkType.DosageAndAdministration, drug.DosageAndAdministration);
        yield return (DrugChunkType.ActiveIngredient, drug.ActiveIngredient);
    }
}
