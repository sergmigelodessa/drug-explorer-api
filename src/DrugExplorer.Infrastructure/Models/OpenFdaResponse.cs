using System.Text.Json.Serialization;

namespace DrugExplorer.Infrastructure.Models;

public class OpenFdaResponse
{
    [JsonPropertyName("meta")]
    public OpenFdaMeta? Meta { get; set; }

    [JsonPropertyName("results")]
    public List<OpenFdaDrugDto> Results { get; set; } = new();
}

public class OpenFdaMeta
{
    [JsonPropertyName("disclaimer")]
    public string? Disclaimer { get; set; }

    [JsonPropertyName("terms")]
    public string? Terms { get; set; }

    [JsonPropertyName("license")]
    public string? License { get; set; }

    [JsonPropertyName("last_updated")]
    public string? LastUpdated { get; set; }

    [JsonPropertyName("results")]
    public OpenFdaMetaResults? Results { get; set; }
}

public class OpenFdaMetaResults
{
    [JsonPropertyName("skip")]
    public int Skip { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }
}
