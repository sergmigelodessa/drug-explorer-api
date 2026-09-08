namespace DrugExplorer.Application.Interfaces;

public interface IChatCompletionService
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
