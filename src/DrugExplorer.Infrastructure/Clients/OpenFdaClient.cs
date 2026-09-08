using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Exceptions;
using DrugExplorer.Domain.Models;
using DrugExplorer.Infrastructure.Mappings;
using DrugExplorer.Infrastructure.Models;

namespace DrugExplorer.Infrastructure.Clients;

public class OpenFdaClient : IOpenFdaClient
{
    private readonly HttpClient _httpClient;
    private readonly IOpenFdaMapper _mapper;
    private readonly ILogger<OpenFdaClient> _logger;

    public OpenFdaClient(
        HttpClient httpClient,
        IOpenFdaMapper mapper,
        ILogger<OpenFdaClient> logger)
    {
        _httpClient = httpClient;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DrugCandidate>> SearchAsync(string query)
    {
        try
        {
            var url = $"drug/label.json?search=openfda.brand_name:{query}&limit=20";

            _logger.LogInformation("Calling OpenFDA API with query: {Query}", query);

            var response = await _httpClient.GetFromJsonAsync<OpenFdaResponse>(url);

            if (response?.Results == null)
            {
                _logger.LogWarning("OpenFDA API returned no results for query: {Query}", query);
                return new List<DrugCandidate>();
            }

            var candidates = response.Results
                .Select(_mapper.Map)
                .ToList();

            _logger.LogInformation("OpenFDA API returned {ResultCount} results for query: {Query}",
                candidates.Count, query);

            return candidates;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogInformation("OpenFDA API returned no matches for query: {Query}", query);
                return new List<DrugCandidate>();
            }

            _logger.LogError(ex, "OpenFDA API request failed for query: {Query}", query);
            throw new ExternalApiException(
                $"Failed to call OpenFDA API: {ex.Message}",
                ex)
            {
                ApiName = "OpenFDA",
                HttpStatusCode = 502
            };
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "OpenFDA API request timeout for query: {Query}", query);
            throw new ExternalApiException(
                "OpenFDA API request timed out",
                ex)
            {
                ApiName = "OpenFDA",
                HttpStatusCode = 504
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize OpenFDA API response for query: {Query}", query);
            throw new ExternalApiException(
                "Invalid response from OpenFDA API",
                ex)
            {
                ApiName = "OpenFDA"
            };
        }
    }
}
