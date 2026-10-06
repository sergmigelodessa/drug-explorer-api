using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugExplorer.Persistence.Migrations
{
    public partial class RenameMedicamentsToDrugs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "Medicaments",
                newName: "Drugs");

            migrationBuilder.Sql(
                "EXEC sp_rename N'[Drugs].[PK_Medicaments]', N'PK_Drugs', N'OBJECT';");

            migrationBuilder.RenameIndex(
                name: "IX_Medicaments_SetId",
                table: "Drugs",
                newName: "IX_Drugs_SetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Drugs_SetId",
                table: "Drugs",
                newName: "IX_Medicaments_SetId");

            migrationBuilder.Sql(
                "EXEC sp_rename N'[Drugs].[PK_Drugs]', N'PK_Medicaments', N'OBJECT';");

            migrationBuilder.RenameTable(
                name: "Drugs",
                newName: "Medicaments");
        }
    }
}
