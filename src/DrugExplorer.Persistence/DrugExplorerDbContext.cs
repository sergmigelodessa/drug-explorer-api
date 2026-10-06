using Microsoft.EntityFrameworkCore;
using DrugExplorer.Domain.Entities;

namespace DrugExplorer.Persistence;

public class DrugExplorerDbContext : DbContext
{
    public DrugExplorerDbContext(DbContextOptions<DrugExplorerDbContext> options)
        : base(options)
    {
    }

    public DbSet<DrugEmbedding> DrugEmbeddings { get; set; }

    public DbSet<Drug> Drugs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Drug>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.OpenFdaId).HasMaxLength(256);
            entity.Property(e => e.SetId).HasMaxLength(256);
            entity.Property(e => e.EffectiveTime).HasMaxLength(32);
            entity.Property(e => e.Version).HasMaxLength(32);
            entity.Property(e => e.BrandName).HasMaxLength(512);
            entity.Property(e => e.GenericName).HasMaxLength(512);
            entity.Property(e => e.ManufacturerName).HasMaxLength(512);
            entity.Property(e => e.DosageForm).HasMaxLength(256);
            entity.Property(e => e.Route).HasMaxLength(256);
            entity.Property(e => e.ProductNdc).HasMaxLength(256);
            entity.Property(e => e.PackageNdc).HasMaxLength(256);
            entity.Property(e => e.ProductType).HasMaxLength(256);
            entity.Property(e => e.SubstanceName).HasMaxLength(512);
            entity.Property(e => e.Rxcui).HasMaxLength(256);
            entity.Property(e => e.SplId).HasMaxLength(256);
            entity.Property(e => e.SplSetId).HasMaxLength(256);
            entity.Property(e => e.PharmClassMoa).HasMaxLength(1024);
            entity.Property(e => e.PharmClassCs).HasMaxLength(1024);
            entity.Property(e => e.PharmClassEpc).HasMaxLength(1024);
            entity.Property(e => e.Unii).HasMaxLength(256);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(e => e.SetId)
                .HasDatabaseName("IX_Drugs_SetId");
        });

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

    }
}
