using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Enums;
using DrugExplorer.Domain.Exceptions;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Infrastructure.VectorSearch;

// Qdrant REST implementation. One point per drug chunk; cosine distance, so scores are cosine similarity.
public class QdrantVectorStore : IVectorStore, IVectorIndexWriter
{
    private const string DISTANCE = "Cosine";

    private readonly HttpClient _httpClient;
    private readonly QdrantOptions _options;
    private readonly ILogger<QdrantVectorStore> _logger;
    private readonly string _collectionPath;

    public QdrantVectorStore(HttpClient httpClient, QdrantOptions options, ILogger<QdrantVectorStore> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
        _collectionPath = $"collections/{Uri.EscapeDataString(options.CollectionName)}";
    }

    public async Task EnsureCollectionAsync(CancellationToken cancellationToken = default)
    {
        using var getResponse = await SendAsync(() => _httpClient.GetAsync(_collectionPath, cancellationToken), cancellationToken);

        if (getResponse.StatusCode == HttpStatusCode.NotFound)
        {
            var body = new { vectors = new { size = _options.VectorSize, distance = DISTANCE } };
            using var createResponse = await SendAsync(() => _httpClient.PutAsJsonAsync(_collectionPath, body, cancellationToken), cancellationToken);
            createResponse.EnsureSuccessStatusCode();

            _logger.LogInformation("Created Qdrant collection {Collection}", _options.CollectionName);
            return;
        }

        getResponse.EnsureSuccessStatusCode();

        var node = await getResponse.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
        var vectors = node?["result"]?["config"]?["params"]?["vectors"];
        var size = vectors?["size"]?.GetValue<int>();
        var distance = vectors?["distance"]?.GetValue<string>();

        if (size != _options.VectorSize || !string.Equals(distance, DISTANCE, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Qdrant collection '{_options.CollectionName}' has size={size}, distance={distance}; expected size={_options.VectorSize}, distance={DISTANCE}.");
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => _httpClient.PostAsJsonAsync($"{_collectionPath}/points/count", new { exact = true }, cancellationToken),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return 0;
        }

        response.EnsureSuccessStatusCode();

        var node = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);
        return node?["result"]?["count"]?.GetValue<int>() ?? 0;
    }

    public async Task<IReadOnlyList<VectorSearchHit>> SearchAsync(float[] queryVector, int topK, CancellationToken cancellationToken = default)
    {
        var request = new { vector = queryVector, limit = topK, with_payload = true };

        using var response = await SendAsync(
            () => _httpClient.PostAsJsonAsync($"{_collectionPath}/points/search", request, cancellationToken),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return Array.Empty<VectorSearchHit>();
        }

        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken);

        return (body?.Result ?? new List<ScoredPoint>())
            .Where(point => point.Payload != null && Guid.TryParse(point.Id, out _))
            .Select(point => new VectorSearchHit(ToEmbedding(point), point.Score))
            .ToList();
    }

    public async Task<HashSet<Guid>> GetExistingIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var request = new { ids, with_payload = false, with_vector = false };

        using var response = await SendAsync(
            () => _httpClient.PostAsJsonAsync($"{_collectionPath}/points", request, cancellationToken),
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<RetrieveResponse>(cancellationToken);

        return (body?.Result ?? new List<RetrievedPoint>())
            .Select(p => Guid.TryParse(p.Id, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
    }

    public async Task UpsertAsync(IReadOnlyCollection<VectorPoint> points, CancellationToken cancellationToken = default)
    {
        if (points.Count == 0)
        {
            return;
        }

        var request = new
        {
            points = points.Select(p => new
            {
                id = p.Embedding.Id,
                vector = p.Vector,
                payload = new
                {
                    DrugKey = p.Embedding.DrugKey,
                    BrandName = p.Embedding.BrandName,
                    GenericName = p.Embedding.GenericName,
                    ChunkType = p.Embedding.ChunkType.ToString(),
                    ChunkText = p.Embedding.ChunkText
                }
            })
        };

        using var response = await SendAsync(
            () => _httpClient.PutAsJsonAsync($"{_collectionPath}/points?wait=true", request, cancellationToken),
            cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static DrugEmbedding ToEmbedding(ScoredPoint point)
    {
        var payload = point.Payload!;
        return new DrugEmbedding
        {
            Id = Guid.Parse(point.Id!),
            DrugKey = payload.DrugKey ?? string.Empty,
            BrandName = payload.BrandName ?? string.Empty,
            GenericName = payload.GenericName ?? string.Empty,
            ChunkType = Enum.TryParse<DrugChunkType>(payload.ChunkType, out var type) ? type : default,
            ChunkText = payload.ChunkText ?? string.Empty
        };
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken cancellationToken)
    {
        try
        {
            return await send();
        }
        catch (Exception ex) when (ex is HttpRequestException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogError(ex, "Qdrant request failed");
            throw new ExternalApiException($"Failed to call Qdrant: {ex.Message}", ex)
            {
                ApiName = "Qdrant",
                HttpStatusCode = 502
            };
        }
    }

    private class SearchResponse
    {
        [JsonPropertyName("result")]
        public List<ScoredPoint>? Result { get; set; }
    }

    private class ScoredPoint
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("score")]
        public double Score { get; set; }

        [JsonPropertyName("payload")]
        public PointPayload? Payload { get; set; }
    }

    private class PointPayload
    {
        public string? DrugKey { get; set; }
        public string? BrandName { get; set; }
        public string? GenericName { get; set; }
        public string? ChunkType { get; set; }
        public string? ChunkText { get; set; }
    }

    private class RetrieveResponse
    {
        [JsonPropertyName("result")]
        public List<RetrievedPoint>? Result { get; set; }
    }

    private class RetrievedPoint
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
