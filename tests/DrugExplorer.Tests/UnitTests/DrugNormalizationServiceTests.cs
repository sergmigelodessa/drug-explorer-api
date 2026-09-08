using DrugExplorer.Application.Services;

namespace DrugExplorer.Tests.UnitTests;

[TestClass]
public class DrugNormalizationServiceTests
{
    private readonly DrugNormalizationService _service = new();

    [TestMethod]
    public void Normalize_WhenInputIsNull_ReturnsEmptyString()
    {
        var result = _service.Normalize(null!);

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Normalize_WhenInputIsWhitespace_ReturnsEmptyString()
    {
        var result = _service.Normalize("   ");

        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Normalize_LowercasesInput()
    {
        var result = _service.Normalize("ASPIRIN");

        Assert.AreEqual("aspirin", result);
    }

    [TestMethod]
    public void Normalize_RemovesDosageInMilligrams()
    {
        var result = _service.Normalize("Ibuprofen 200mg");

        Assert.AreEqual("ibuprofen", result);
    }

    [TestMethod]
    public void Normalize_RemovesDosageInMilliliters()
    {
        var result = _service.Normalize("Cough Syrup 15ml");

        Assert.AreEqual("cough syrup", result);
    }

    [TestMethod]
    public void Normalize_RemovesCommonDosageFormWords()
    {
        var result = _service.Normalize("Vitamin C Gel");

        Assert.AreEqual("vitamin c", result);
    }

    [TestMethod]
    public void Normalize_CollapsesExtraWhitespace_AndTrims()
    {
        var result = _service.Normalize("  Acetaminophen   500mg   Tablet  ");

        Assert.AreEqual("acetaminophen", result);
    }
}
