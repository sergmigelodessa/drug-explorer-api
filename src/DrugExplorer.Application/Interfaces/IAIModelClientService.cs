namespace DrugExplorer.Application.Interfaces;

public interface IAIModelClientService
{
    Task<string> SendPrompt(string prompt, CancellationToken cancellationToken = default);
}
