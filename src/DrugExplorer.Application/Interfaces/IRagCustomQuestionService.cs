using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Interfaces;

public interface IRagCustomQuestionService
{
    Task<RagAnswer> DoRequestAsync(string question, CancellationToken cancellationToken = default);
}
