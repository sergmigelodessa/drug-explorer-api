using DrugExplorer.Application.Services;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Tests.UnitTests;

[TestClass]
public class DrugGroupingServiceTests
{
    private readonly DrugGroupingService _service = new();

    [TestMethod]
    public void Group_WhenVariantsIsNull_ReturnsEmptyList()
    {
        var result = _service.Group(null!);

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Group_WhenVariantsIsEmpty_ReturnsEmptyList()
    {
        var result = _service.Group(new List<DrugCandidate>());

        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public void Group_GroupsCandidatesByBrandName_CaseInsensitive()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "Aspirin", GenericName = "Acetylsalicylic Acid", Manufacturer = "Pharma A" },
            new() { BrandName = "aspirin", GenericName = "Acetylsalicylic Acid", Manufacturer = "Pharma B" }
        };

        var result = _service.Group(candidates);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("aspirin", result[0].GroupName);
        Assert.AreEqual(2, result[0].TotalVariants);
    }

    [TestMethod]
    public void Group_WhenBrandNameMissing_FallsBackToGenericName()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "", GenericName = "Ibuprofen" }
        };

        var result = _service.Group(candidates);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("ibuprofen", result[0].GroupName);
    }

    [TestMethod]
    public void Group_WhenBrandAndGenericMissing_UsesUnknownKey()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "", GenericName = "" }
        };

        var result = _service.Group(candidates);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("unknown", result[0].GroupName);
    }

    [TestMethod]
    public void Group_MapsVariantFieldsFromCandidates()
    {
        var candidates = new List<DrugCandidate>
        {
            new()
            {
                BrandName = "Tylenol",
                GenericName = "Acetaminophen",
                Manufacturer = "Pharma C",
                DosageForm = "Tablet",
                Route = "Oral",
                Purpose = "Pain relief",
                Warnings = "Do not exceed dose",
                ActiveIngredient = "Acetaminophen",
                ProductType = "OTC",
                ApplicationNumber = "12345"
            }
        };

        var result = _service.Group(candidates);
        var variant = result[0].Variants[0];

        Assert.AreEqual("Tylenol", variant.Name);
        Assert.AreEqual("Tablet", variant.DosageForm);
        Assert.AreEqual("Pharma C", variant.Manufacturer);
        Assert.AreEqual("Oral", variant.Route);
        Assert.AreEqual("Pain relief", variant.Purpose);
        Assert.AreEqual("Do not exceed dose", variant.Warnings);
        Assert.AreEqual("Acetaminophen", variant.ActiveIngredient);
        Assert.AreEqual("OTC", variant.ProductType);
    }

    [TestMethod]
    public void Group_OrdersGroupsByScoreDescending()
    {
        var candidates = new List<DrugCandidate>
        {
            new() { BrandName = "SoloDrug" },
            new() { BrandName = "PopularDrug", Manufacturer = "Pharma A", DosageForm = "Tablet", Purpose = "Relief" },
            new() { BrandName = "PopularDrug", Manufacturer = "Pharma A", DosageForm = "Tablet", Purpose = "Relief" }
        };

        var result = _service.Group(candidates);

        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("populardrug", result[0].GroupName);
        Assert.AreEqual("solodrug", result[1].GroupName);
    }
}
