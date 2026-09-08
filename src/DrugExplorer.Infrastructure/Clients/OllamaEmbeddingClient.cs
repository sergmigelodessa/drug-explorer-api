using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Exceptions;

namespace DrugExplorer.Infrastructure.Clients;

public class OllamaEmbeddingClient : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly ILogger<OllamaEmbeddingClient> _logger;

    public OllamaEmbeddingClient(HttpClient httpClient, string model, ILogger<OllamaEmbeddingClient> logger)
    {
        _httpClient = httpClient;
        _model = model;
        _logger = logger;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                "api/embeddings",
                new OllamaEmbeddingRequest { Model = _model, Prompt = text },
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var body = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken);

            return body?.Embedding ?? throw new ExternalApiException("Ollama returned an empty embedding")
            {
                ApiName = "Ollama"
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Ollama embedding request failed");
            throw new ExternalApiException($"Failed to call Ollama embeddings API: {ex.Message}", ex)
            {
                ApiName = "Ollama",
                HttpStatusCode = 502
            };
        }
    }

    private class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}
