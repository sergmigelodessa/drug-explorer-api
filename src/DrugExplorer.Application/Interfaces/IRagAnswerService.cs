using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IRagAnswerService
{
    Task<RagAnswer> AskAsync(string question, CancellationToken cancellationToken = default);
}
