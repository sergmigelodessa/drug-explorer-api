using Microsoft.EntityFrameworkCore;
using DrugExplorer.Api.Mappings;
using DrugExplorer.Api.Middleware;
using DrugExplorer.Application.Interfaces;
using DrugExplorer.Application.Services;
using DrugExplorer.Infrastructure.Clients;
using DrugExplorer.Infrastructure.Mappings;
using DrugExplorer.Infrastructure.VectorSearch;
using DrugExplorer.Persistence;
using DrugExplorer.Persistence.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.AddMemoryCache();

// Add Swagger/OpenAPI
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "DrugExplorer API",
        Version = "v1",
        Description = "API for searching drugs from OpenFDA database with caching and analytics",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "DrugExplorer",
            Url = new Uri("https://github.com/")
        }
    });
});
builder.Services.AddEndpointsApiExplorer();

// Add DbContext
builder.Services.AddDbContext<DrugExplorerDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient<IOpenFdaClient, OpenFdaClient>(client =>
{
    client.BaseAddress = new Uri("https://api.fda.gov/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

var ollamaBaseUrl = builder.Configuration["Ollama:BaseUrl"] ?? "http://localhost:11434/";
var ollamaEmbeddingModel = builder.Configuration["Ollama:EmbeddingModel"] ?? "nomic-embed-text";
var ollamaChatModel = builder.Configuration["Ollama:ChatModel"] ?? "qwen2.5:1.5b";

builder.Services.AddHttpClient<IEmbeddingService, OllamaEmbeddingClient>(client =>
{
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddTypedClient<IEmbeddingService>((httpClient, sp) => new OllamaEmbeddingClient(
    httpClient,
    ollamaEmbeddingModel,
    sp.GetRequiredService<ILogger<OllamaEmbeddingClient>>()));

builder.Services.AddHttpClient<IChatCompletionService, OllamaChatClient>(client =>
{
    client.BaseAddress = new Uri(ollamaBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(180);
})
.AddTypedClient<IChatCompletionService>((httpClient, sp) => new OllamaChatClient(
    httpClient,
    ollamaChatModel,
    sp.GetRequiredService<ILogger<OllamaChatClient>>()));

var qdrantOptions = builder.Configuration.GetSection("Qdrant").Get<QdrantOptions>() ?? new QdrantOptions();

builder.Services.AddHttpClient<QdrantVectorStore>(client =>
{
    client.BaseAddress = new Uri(qdrantOptions.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(qdrantOptions.TimeoutSeconds);
    if (!string.IsNullOrEmpty(qdrantOptions.ApiKey))
    {
        client.DefaultRequestHeaders.Add("api-key", qdrantOptions.ApiKey);
    }
})
.AddTypedClient((httpClient, sp) => new QdrantVectorStore(
    httpClient,
    qdrantOptions,
    sp.GetRequiredService<ILogger<QdrantVectorStore>>()));
builder.Services.AddTransient<IVectorStore>(sp => sp.GetRequiredService<QdrantVectorStore>());
builder.Services.AddTransient<IVectorIndexWriter>(sp => sp.GetRequiredService<QdrantVectorStore>());

builder.Services.AddScoped<IDrugNormalizationService, DrugNormalizationService>();
builder.Services.AddScoped<IDrugGroupingService, DrugGroupingService>();
builder.Services.AddScoped<IDrugSearchService, DrugSearchService>();
builder.Services.AddScoped<IOpenFdaMapper, OpenFdaMapper>();
builder.Services.AddScoped<IDrugEmbeddingRepository, DrugEmbeddingRepository>();
builder.Services.AddScoped<IMedicamentRepository, MedicamentRepo>();
builder.Services.AddScoped<IDrugKnowledgeIngestionService, DrugKnowledgeIngestionService>();
builder.Services.AddScoped<IMedicamentSeedService, MedicamentSeedService>();
builder.Services.AddScoped<IVectorIndexBuildService, VectorIndexBuildService>();
builder.Services.AddScoped<ISemanticSearchService, SemanticSearchService>();
builder.Services.AddScoped<IRagAnswerService, RagAnswerService>();
builder.Services.AddScoped<DrugResultMapper>();

var app = builder.Build();

// Apply pending migrations at startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<DrugExplorerDbContext>();
    dbContext.Database.Migrate();
}

// Create the Qdrant collection if missing; don't block startup when Qdrant is down.
using (var scope = app.Services.CreateScope())
{
    try
    {
        await scope.ServiceProvider.GetRequiredService<IVectorIndexWriter>().EnsureCollectionAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Qdrant collection check failed at startup");
    }
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "DrugExplorer API v1");
    c.RoutePrefix = "swagger";
});

app.UseGlobalExceptionMiddleware();
app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.MapControllers();

app.Run();
