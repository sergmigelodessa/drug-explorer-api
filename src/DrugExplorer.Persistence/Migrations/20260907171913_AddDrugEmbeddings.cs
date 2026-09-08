using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugExplorer.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDrugEmbeddings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DrugEmbeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DrugKey = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BrandName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ChunkType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ChunkText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmbeddingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrugEmbeddings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DrugEmbeddings_DrugKey_ChunkType",
                table: "DrugEmbeddings",
                columns: new[] { "DrugKey", "ChunkType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DrugEmbeddings");
        }
    }
}
