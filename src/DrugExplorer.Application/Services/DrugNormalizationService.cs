using DrugExplorer.Application.Interfaces;

namespace DrugExplorer.Application.Services;

public class DrugNormalizationService : IDrugNormalizationService
{
    public string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var text = input.ToLowerInvariant();

        // remove dosage
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\d+\s?mg|\d+\s?ml", "");

        // remove common words
        text = text.Replace("caps", "")
                   .Replace("capsules", "")
                   .Replace("tablet", "")
                   .Replace("tablets", "")
                   .Replace("liqui", "")
                   .Replace("gel", "")
                   .Replace("gels", "");

        // cleanup
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        return text.Trim();
    }
}
