using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrugExplorer.Persistence.Migrations
{
    public partial class AddMedicaments : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Medicaments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenFdaId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SetId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EffectiveTime = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Version = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    BrandName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    GenericName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ManufacturerName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    DosageForm = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Route = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ProductNdc = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PackageNdc = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SubstanceName = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Rxcui = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SplId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SplSetId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsOriginalPackager = table.Column<bool>(type: "bit", nullable: true),
                    PharmClassMoa = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    PharmClassCs = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    PharmClassEpc = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Unii = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActiveIngredient = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndicationsAndUsage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Warnings = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DoNotUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AskDoctor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AskDoctorOrPharmacist = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WhenUsing = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StopUse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PregnancyOrBreastFeeding = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    KeepOutOfReachOfChildren = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DosageAndAdministration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DosageAndAdministrationTable = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InactiveIngredient = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SplProductDataElements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SplUnclassifiedSection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackageLabelPrincipalDisplayPanel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RecentMajorChanges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicaments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Medicaments_SetId",
                table: "Medicaments",
                column: "SetId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Medicaments");
        }
    }
}