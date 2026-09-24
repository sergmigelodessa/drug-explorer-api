# DrugExplorer Backend

## Overview

The backend is a layered .NET solution:

```text
DrugExplorer.Api -> DrugExplorer.Application -> DrugExplorer.Domain
                         ^                         ^
                         |                         |
                Persistence / Infrastructure ------
```

`Domain` has no dependencies on other layers. `Application` owns business orchestration and interfaces. `Persistence` owns EF Core and SQL Server. `Infrastructure` owns external HTTP clients, mappings, and vector-search implementations. `Api` owns HTTP, DTOs, middleware, and dependency-injection wiring.

## Projects

| Project | Responsibility |
|---|---|
| `src/DrugExplorer.Api` | Controllers, HTTP DTOs, API mappings, middleware, `Program.cs` |
| `src/DrugExplorer.Application` | Services and interfaces; no HTTP or EF Core references |
| `src/DrugExplorer.Domain` | Entities, enums, value objects, domain models, exceptions |
| `src/DrugExplorer.Persistence` | `DrugExplorerDbContext`, migrations, SQL repositories |
| `src/DrugExplorer.Infrastructure` | OpenFDA, Ollama, vector stores, external mappings |
| `tests/DrugExplorer.Tests` | MSTest unit and integration tests |

## Main endpoints

Base route: `api/drugs`

| Method | Route | Application service | Purpose |
|---|---|---|---|
| `GET` | `/search?query=` | `IDrugSearchService` | OpenFDA search, grouping, caching, history, background ingestion |
| `GET` | `/semantic-search?query=&topK=` | `ISemanticSearchService` | Embedding-based search over drug chunks |
| `POST` | `/ask` | `IRagAnswerService` | Grounded answer from retrieved label chunks |
| `GET` | `/analytics` | search-history repository | Aggregate search metrics |

Swagger is available at `/swagger` when the API is running.

## Current runtime wiring

- SQL Server stores drug search history and `DrugEmbeddings`.
- `DrugEmbedding.EmbeddingJson` stores 768-dimensional vectors as JSON.
- `InMemoryVectorStore` is registered as a singleton `IVectorStore`.
- The vector store loads all embeddings at startup and reloads after ingestion.
- `OllamaEmbeddingClient` uses `nomic-embed-text` at `http://localhost:11434/`.
- `OllamaChatClient` uses `qwen2.5:1.5b`; its timeout is 180 seconds because model loading can be slow.
- OpenFDA is accessed through a typed client with a 10-second timeout.

Startup applies EF migrations and then calls `IVectorStore.ReloadAsync()`.

## Request flows

### Drug search

`DrugsController` -> `DrugSearchService` -> normalization -> memory cache -> OpenFDA on cache miss -> grouping -> search history. A cache miss also starts bounded background knowledge ingestion in its own DI scope. Ingestion extracts label sections, deduplicates `(DrugKey, ChunkType)`, creates Ollama embeddings, writes SQL, and reloads the in-memory vector store.

### Semantic search

`SemanticSearchService` embeds the query, asks `IVectorStore` for `topK * 5` cosine-similarity hits, groups hits by `DrugKey`, keeps the best hit per drug, and returns the requested number of drug results.

### RAG ask

`RagAnswerService` embeds the question, retrieves up to five chunks, keeps hits with similarity at least `0.35`, and builds a cited prompt for Ollama. With no qualifying context it returns a non-grounded fallback instead of guessing. Successful answers include source chunk text and similarity values.

## Persistence

`DrugEmbeddings` has a unique index on `(DrugKey, ChunkType)`. Search history is indexed for creation time, normalized query, and the combined query/time lookup. Repositories use `DrugExplorerDbContext` directly; there is no generic repository abstraction.

## Error handling

`GlobalExceptionMiddleware` maps `ExternalApiException` to its configured HTTP status (normally 502), `SearchProcessingException` and `ArgumentException` to 400, and unexpected exceptions to 500. Responses include timestamp, trace ID, error code, and details.

## Useful commands

Run from `DrugExplorer/`:

```powershell
dotnet build DrugExplorer.sln
dotnet test tests/DrugExplorer.Tests/DrugExplorer.Tests.csproj
dotnet run --project src/DrugExplorer.Api/DrugExplorer.Api.csproj
```

Before using `dotnet run --no-build`, rebuild after code changes, migrations, or dependency changes so stale binaries are not mistaken for current behavior.

## Vector-search direction

Qdrant is installed in the WSL2 distribution `DrugExplorerUbuntu`, exposed at `http://localhost:6333`. It is not wired into the API yet. The intended architecture keeps SQL Server as the source of drug metadata and chunks while Qdrant stores searchable vectors and payload. See [qdrant-migration.md](qdrant-migration.md).
