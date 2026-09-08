using Microsoft.EntityFrameworkCore;
using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Persistence;

public class DrugExplorerDbContext : DbContext
{
    public DrugExplorerDbContext(DbContextOptions<DrugExplorerDbContext> options)
        : base(options)
    {
    }

    public DbSet<DrugSearchHistory> SearchHistory { get; set; }

    public DbSet<DrugEmbedding> DrugEmbeddings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DrugEmbedding
        modelBuilder.Entity<DrugEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DrugKey)
                .IsRequired()
                .HasMaxLength(512);

            entity.Property(e => e.BrandName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.GenericName)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.ChunkType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(64);

            entity.Property(e => e.ChunkText)
                .IsRequired();

            entity.Property(e => e.EmbeddingJson)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => new { e.DrugKey, e.ChunkType })
                .IsUnique()
                .HasDatabaseName("IX_DrugEmbeddings_DrugKey_ChunkType");
        });

        // Configure DrugSearchHistory
        modelBuilder.Entity<DrugSearchHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.QueryText)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.NormalizedQuery)
                .IsRequired()
                .HasMaxLength(256);

            entity.Property(e => e.ResultCount)
                .IsRequired();

            entity.Property(e => e.ExecutionTimeMs)
                .IsRequired();

            entity.Property(e => e.CacheHit)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            // Create indices for common queries
            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_SearchHistory_CreatedAt");

            entity.HasIndex(e => e.NormalizedQuery)
                .HasDatabaseName("IX_SearchHistory_NormalizedQuery");

            entity.HasIndex(e => new { e.NormalizedQuery, e.CreatedAt })
                .HasDatabaseName("IX_SearchHistory_NormalizedQuery_CreatedAt");
        });
    }
}
