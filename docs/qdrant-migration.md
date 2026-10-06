# Qdrant Migration

## Current state

- SQL Server stores `Drugs` and `DrugEmbeddings`.
- The API uses `QdrantVectorStore` (collection `drug_chunks`) for semantic search and RAG; `InMemoryVectorStore` is no longer registered.
- New ingestion chunks are upserted to Qdrant; no full reload is needed.
- `POST /api/drugs/build-vector-index` backfills Qdrant from the `Drugs` table.
- Qdrant 1.19.1 is installed in WSL2 distribution `DrugExplorerUbuntu`.
- The WSL2 virtual disk is stored at `D:\WSL\DrugExplorerUbuntu\ext4.vhdx`.
- Qdrant API is available at `http://localhost:6333`.

## Migration plan

- [x] Define collection contract: name, 768 dimensions, `Cosine`, and payload fields `DrugKey`, `BrandName`, `GenericName`, `ChunkType`, `ChunkText` (point id is a deterministic GUID, not the SQL `Id`).
- [x] Add configurable Qdrant URL, collection name, timeout, and optional API key to application settings.
- [x] Add a small Infrastructure HTTP client. Keep Qdrant types out of Domain and Application.
- [x] Introduce an application vector-search abstraction returning the existing `VectorSearchHit` shape or an equivalent domain model.
- [x] Create and validate the collection at startup; report incompatible vector size or distance clearly.
- [x] Add an idempotent backfill from SQL Server using deterministic point IDs (`VectorIndexBuildService`; processes one chunk at a time).
- [ ] Add a verification report for SQL/Qdrant counts, malformed embeddings, dimensions, and sample similarity queries.
- [x] Change `RagAnswerService` to query Qdrant with the existing top-five and `0.35` threshold behavior.
- [x] Change `SemanticSearchService` to query Qdrant while preserving grouping by `DrugKey` and the public response contract.
- [x] Upsert new ingestion chunks to Qdrant and remove the full reload dependency.
- [ ] Add retry, timeout, and error mapping; decide whether rollout uses fallback or fail-closed behavior.
- [ ] Add integration tests for collection creation, upsert, search, payload mapping, idempotency, and unavailable Qdrant.
- [ ] Run both implementations in shadow mode and compare representative top-K results.
- [x] Switch DI from `InMemoryVectorStore` to Qdrant.
- [x] Remove startup reload (the `InMemoryVectorStore` class is kept for now).
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
