# Qdrant Migration

## Current state

- SQL Server stores `DrugEmbeddings`, including `EmbeddingJson` as JSON.
- The API uses singleton `InMemoryVectorStore` and loads every embedding into RAM at startup.
- New chunks trigger a full vector-store reload after ingestion.
- Qdrant 1.19.1 is installed in WSL2 distribution `DrugExplorerUbuntu`.
- The WSL2 virtual disk is stored at `D:\WSL\DrugExplorerUbuntu\ext4.vhdx`.
- Qdrant API is available at `http://localhost:6333`.
- Qdrant currently has no collections and is not used by the API.

## Migration plan

- [ ] Define collection contract: name, 768 dimensions, `Cosine`, and payload fields `DrugKey`, `BrandName`, `GenericName`, `ChunkType`, `ChunkText`, and SQL `Id`.
- [ ] Add configurable Qdrant URL, collection name, timeout, and optional API key to application settings.
- [ ] Add a small Infrastructure HTTP client. Keep Qdrant types out of Domain and Application.
- [ ] Introduce an application vector-search abstraction returning the existing `VectorSearchHit` shape or an equivalent domain model.
- [ ] Create and validate the collection at startup; report incompatible vector size or distance clearly.
- [ ] Add an idempotent batch backfill from SQL Server using deterministic point IDs based on `DrugEmbedding.Id`.
- [ ] Add a verification report for SQL/Qdrant counts, malformed embeddings, dimensions, and sample similarity queries.
- [ ] Change `RagAnswerService` to query Qdrant with the existing top-five and `0.35` threshold behavior.
- [ ] Change `SemanticSearchService` to query Qdrant while preserving grouping by `DrugKey` and the public response contract.
- [ ] Upsert new ingestion chunks to Qdrant and remove the full reload dependency.
- [ ] Add retry, timeout, and error mapping; decide whether rollout uses fallback or fail-closed behavior.
- [ ] Add integration tests for collection creation, upsert, search, payload mapping, idempotency, and unavailable Qdrant.
- [ ] Run both implementations in shadow mode and compare representative top-K results.
- [ ] Switch DI from `InMemoryVectorStore` to Qdrant.
- [ ] Remove startup reload and in-memory implementation only after verification.
- [ ] Document Qdrant backup, restore, and long-term data ownership.

## Acceptance criteria

- [ ] API startup does not load all embeddings into process memory.
- [ ] Semantic search and RAG return equivalent or better results.
- [ ] New ingestion is searchable without a full reload.
- [ ] SQL and Qdrant counts match after backfill.
- [ ] Restarting WSL/Qdrant preserves the collection and points.
- [ ] URL and collection name are configurable; no local path is hard-coded in application code.

## Investigation notes

The first implementation should preserve `IVectorStore` at the application boundary if that keeps the change small. The Infrastructure implementation can translate Qdrant responses into `VectorSearchHit`. This lets `RagAnswerService` and `SemanticSearchService` retain their current behavior while storage changes underneath them.
