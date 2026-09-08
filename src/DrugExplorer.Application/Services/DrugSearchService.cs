using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Domain.Entities;
using DrugExplorer.Domain.Models;

namespace DrugExplorer.Application.Services;

public class DrugSearchService : IDrugSearchService
{
    private readonly IDrugNormalizationService _normalizer;
    private readonly IOpenFdaClient _client;
    private readonly IDrugGroupingService _groupingService;
    private readonly IMemoryCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DrugSearchService> _logger;
    private readonly IDrugSearchHistoryRepository? _historyRepository;
    private readonly IServiceScopeFactory _scopeFactory;

    private const string CACHE_KEY_PREFIX = "drug_search_";
    private const int DEFAULT_CACHE_TTL_MINUTES = 10;
    private const bool DEFAULT_ENABLE_CACHING = true;

    public DrugSearchService(
        IDrugNormalizationService normalizer,
        IOpenFdaClient client,
        IDrugGroupingService groupingService,
        IMemoryCache cache,
        IConfiguration configuration,
        ILogger<DrugSearchService> logger,
        IServiceScopeFactory scopeFactory,
        IDrugSearchHistoryRepository? historyRepository = null)
    {
        _normalizer = normalizer;
        _client = client;
        _groupingService = groupingService;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;
        _scopeFactory = scopeFactory;
        _historyRepository = historyRepository;
    }

    public async Task<DrugSearchResult> SearchAsync(string query)
    {
        var stopwatch = Stopwatch.StartNew();
        var isCacheHit = false;

        try
        {
            // 1. Normalize input
            var normalized = _normalizer.Normalize(query);

            // 2. Check cache
            var cacheKey = GenerateCacheKey(normalized);
            var isCachingEnabled = GetCachingEnabled();

            if (isCachingEnabled && _cache.TryGetValue<DrugSearchResult>(cacheKey, out var cachedResult))
            {
                _logger.LogInformation("Cache hit for normalized query: {NormalizedQuery}", normalized);
                isCacheHit = true;
                stopwatch.Stop();

                await SaveSearchHistoryAsync(query, normalized, cachedResult!.Groups.Count, stopwatch.ElapsedMilliseconds, isCacheHit);

                return cachedResult!;
            }

            _logger.LogInformation("Cache miss for normalized query: {NormalizedQuery}. Fetching from API...", normalized);

            // 3. Call external API
            var variants = await _client.SearchAsync(normalized);

            // Fire-and-forget knowledge base ingestion so it never adds latency to the search response.
            IngestKnowledgeInBackground(variants);

            // 4. Group results
            var groups = _groupingService.Group(variants);

            // 5. Build result
            var result = new DrugSearchResult
            {
                NormalizedQuery = normalized,
                Groups = groups
            };

            // 6. Cache result if caching is enabled
            if (isCachingEnabled)
            {
                var cacheTtlMinutes = GetCacheTtlMinutes();
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheTtlMinutes));

                _cache.Set(cacheKey, result, cacheOptions);
                _logger.LogInformation("Search result cached for {CacheTtlMinutes} minutes. Cache key: {CacheKey}",
                    cacheTtlMinutes, cacheKey);
            }

            stopwatch.Stop();

            await SaveSearchHistoryAsync(query, normalized, groups.Count, stopwatch.ElapsedMilliseconds, isCacheHit);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Error during drug search for query: {Query}", query);
            throw;
        }
    }

    private string GenerateCacheKey(string normalizedQuery)
    {
        return $"{CACHE_KEY_PREFIX}{normalizedQuery.GetHashCode():X}";
    }

    private void IngestKnowledgeInBackground(IReadOnlyList<DrugCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                // New DI scope: the request's scoped DbContext is disposed once the response completes.
                using var scope = _scopeFactory.CreateScope();
                var ingestionService = scope.ServiceProvider.GetRequiredService<IDrugKnowledgeIngestionService>();
                await ingestionService.IngestAsync(candidates);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background drug knowledge ingestion failed");
            }
        });
    }

    private bool GetCachingEnabled()
    {
        var value = _configuration["CacheSettings:EnableCaching"];
        if (bool.TryParse(value, out var result))
        {
            return result;
        }
        return DEFAULT_ENABLE_CACHING;
    }

    private int GetCacheTtlMinutes()
    {
        var value = _configuration["CacheSettings:SearchCacheTtlMinutes"];
        if (int.TryParse(value, out var result))
        {
            return result;
        }
        return DEFAULT_CACHE_TTL_MINUTES;
    }

    private async Task SaveSearchHistoryAsync(
        string queryText,
        string normalizedQuery,
        int resultCount,
        long executionTimeMs,
        bool cacheHit)
    {
        try
        {
            // Skip if repository not injected (optional dependency)
            if (_historyRepository == null)
            {
                return;
            }

            var history = new DrugSearchHistory
            {
                QueryText = queryText,
                NormalizedQuery = normalizedQuery,
                ResultCount = resultCount,
                ExecutionTimeMs = (int)executionTimeMs,
                CacheHit = cacheHit,
                CreatedAt = DateTime.UtcNow
            };

            await _historyRepository.SaveSearchAsync(history);
            _logger.LogDebug("Saved search history for query: {QueryText}", queryText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to save search history for query: {QueryText}", queryText);
        }
    }
}
