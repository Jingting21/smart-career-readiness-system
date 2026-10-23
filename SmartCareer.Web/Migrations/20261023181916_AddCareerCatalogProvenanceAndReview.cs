using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCareer.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerCatalogProvenanceAndReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalReviewNote",
                table: "Careers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresLocalReview",
                table: "Careers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CareerCatalogSources",
                columns: table => new
                {
                    CareerCatalogSourceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SourceName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    OfficialSourceUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    LicenseName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LicenseUrl = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: false),
                    RetrievedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    AttributionText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerCatalogSources", x => x.CareerCatalogSourceId);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Careers_LocalReview",
                table: "Careers",
                sql: "([RequiresLocalReview] = 0 AND [LocalReviewNote] IS NULL) OR ([RequiresLocalReview] = 1 AND [LocalReviewNote] IS NOT NULL AND LEN(LTRIM(RTRIM(REPLACE(REPLACE(REPLACE([LocalReviewNote], CHAR(9), N''), CHAR(10), N''), CHAR(13), N'')))) > 0)");

            migrationBuilder.CreateIndex(
                name: "IX_CareerCatalogSources_SourceName_SourceVersion",
                table: "CareerCatalogSources",
                columns: new[] { "SourceName", "SourceVersion" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerCatalogSources");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Careers_LocalReview",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "LocalReviewNote",
                table: "Careers");

            migrationBuilder.DropColumn(
                name: "RequiresLocalReview",
                table: "Careers");
        }
    }
}
