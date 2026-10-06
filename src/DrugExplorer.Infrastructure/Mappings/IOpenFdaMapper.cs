using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Models;
using DrugExplorer.Infrastructure.Models;

namespace DrugExplorer.Infrastructure.Mappings;

public interface IOpenFdaMapper
{
    DrugCandidate Map(OpenFdaDrugDto dto);

    Drug MapToDrug(OpenFdaDrugDto dto);
}
