using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugExplorer.Persistence.Migrations;

public partial class RemoveSearchHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "SearchHistory");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "SearchHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QueryText = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                NormalizedQuery = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ResultCount = table.Column<int>(type: "int", nullable: false),
                ExecutionTimeMs = table.Column<int>(type: "int", nullable: false),
                CacheHit = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SearchHistory", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_SearchHistory_CreatedAt",
            table: "SearchHistory",
            column: "CreatedAt");

        migrationBuilder.CreateIndex(
            name: "IX_SearchHistory_NormalizedQuery",
            table: "SearchHistory",
            column: "NormalizedQuery");

        migrationBuilder.CreateIndex(
            name: "IX_SearchHistory_NormalizedQuery_CreatedAt",
            table: "SearchHistory",
            columns: new[] { "NormalizedQuery", "CreatedAt" });
    }
}
