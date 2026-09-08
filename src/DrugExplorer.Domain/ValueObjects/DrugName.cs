namespace DrugExplorer.Domain.ValueObjects;

public class DrugName
{
    public string Value { get; }

    public DrugName(string value)
    {
        Value = Normalize(value);
    }

    private string Normalize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return input
            .ToLower()
            .Replace("caps", "")
            .Replace("tablet", "")
            .Replace("liqui", "")
            .Trim();
    }

    public override string ToString() => Value;
}
