using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Exceptions;

namespace DrugExplorer.Infrastructure.Clients;

public class OllamaChatClient : IChatCompletionService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OllamaChatClient> _logger;

    public OllamaChatClient(HttpClient httpClient, string model, ILogger<OllamaChatClient> logger)
    {
        _httpClient = httpClient;
        _model = model;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/generate",
                new OllamaGenerateRequest { Model = _model, Prompt = prompt, Stream = false },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(cancellationToken);

            return body?.Response?.Trim() ?? string.Empty;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama chat completion request failed");
            throw new ExternalApiException($"Failed to call Ollama generate API: {ex.Message}", ex)
            {
                ApiName = "Ollama",
                HttpStatusCode = 502
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Ollama chat completion request timed out");
            throw new ExternalApiException("Ollama generate API request timed out", ex)
            {
                ApiName = "Ollama",
                HttpStatusCode = 504
            };
        }
    }

    private class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }
    }
}
