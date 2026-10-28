using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCareer.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddEducationAndPreparationSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerEducationRatings",
                columns: table => new
                {
                    CareerEducationRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    ElementId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ScaleId = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    CategoryDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    RespondentCount = table.Column<int>(type: "int", nullable: true),
                    StandardError = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    LowerConfidenceBound = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    UpperConfidenceBound = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    IsSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    SourceUpdatedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DomainSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerEducationRatings", x => x.CareerEducationRatingId);
                    table.CheckConstraint("CK_CareerEducationRatings_Category", "[Category] > 0");
                    table.CheckConstraint("CK_CareerEducationRatings_Percentage", "[Percentage] IS NULL OR ([Percentage] >= 0 AND [Percentage] <= 100)");
                    table.CheckConstraint("CK_CareerEducationRatings_RespondentCount", "[RespondentCount] IS NULL OR [RespondentCount] >= 0");
                    table.CheckConstraint("CK_CareerEducationRatings_ScaleId", "[ScaleId] IN ('RL', 'RQ')");
                    table.ForeignKey(
                        name: "FK_CareerEducationRatings_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "CareerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerTrainingRatings",
                columns: table => new
                {
                    CareerTrainingRatingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    ElementId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ScaleId = table.Column<string>(type: "varchar(3)", unicode: false, maxLength: 3, nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    CategoryDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Percentage = table.Column<decimal>(type: "decimal(6,2)", precision: 6, scale: 2, nullable: true),
                    RespondentCount = table.Column<int>(type: "int", nullable: true),
                    StandardError = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    LowerConfidenceBound = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    UpperConfidenceBound = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    IsSuppressed = table.Column<bool>(type: "bit", nullable: false),
                    SourceUpdatedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DomainSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerTrainingRatings", x => x.CareerTrainingRatingId);
                    table.CheckConstraint("CK_CareerTrainingRatings_Category", "[Category] > 0");
                    table.CheckConstraint("CK_CareerTrainingRatings_Percentage", "[Percentage] IS NULL OR ([Percentage] >= 0 AND [Percentage] <= 100)");
                    table.CheckConstraint("CK_CareerTrainingRatings_RespondentCount", "[RespondentCount] IS NULL OR [RespondentCount] >= 0");
                    table.CheckConstraint("CK_CareerTrainingRatings_ScaleId", "[ScaleId] IN ('RW', 'PT', 'OJ')");
                    table.ForeignKey(
                        name: "FK_CareerTrainingRatings_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "CareerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobZoneDefinitions",
                columns: table => new
                {
                    JobZone = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Education = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    JobTraining = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Examples = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SvpRange = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    SourceVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobZoneDefinitions", x => x.JobZone);
                    table.CheckConstraint("CK_JobZoneDefinitions_JobZone", "[JobZone] BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "CareerJobZones",
                columns: table => new
                {
                    CareerJobZoneId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    JobZone = table.Column<int>(type: "int", nullable: false),
                    SourceUpdatedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DomainSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerJobZones", x => x.CareerJobZoneId);
                    table.ForeignKey(
                        name: "FK_CareerJobZones_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "CareerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerJobZones_JobZoneDefinitions_JobZone",
                        column: x => x.JobZone,
                        principalTable: "JobZoneDefinitions",
                        principalColumn: "JobZone",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerEducationRatings_CareerId_ElementId_ScaleId_Category",
                table: "CareerEducationRatings",
                columns: new[] { "CareerId", "ElementId", "ScaleId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerEducationRatings_CareerId_ScaleId_Percentage",
                table: "CareerEducationRatings",
                columns: new[] { "CareerId", "ScaleId", "Percentage" });

            migrationBuilder.CreateIndex(
                name: "IX_CareerJobZones_CareerId",
                table: "CareerJobZones",
                column: "CareerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerJobZones_JobZone_CareerId",
                table: "CareerJobZones",
                columns: new[] { "JobZone", "CareerId" });

            migrationBuilder.CreateIndex(
                name: "IX_CareerTrainingRatings_CareerId_ElementId_ScaleId_Category",
                table: "CareerTrainingRatings",
                columns: new[] { "CareerId", "ElementId", "ScaleId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerTrainingRatings_CareerId_ElementId_ScaleId_Percentage",
                table: "CareerTrainingRatings",
                columns: new[] { "CareerId", "ElementId", "ScaleId", "Percentage" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerEducationRatings");

            migrationBuilder.DropTable(
                name: "CareerJobZones");

            migrationBuilder.DropTable(
                name: "CareerTrainingRatings");

            migrationBuilder.DropTable(
                name: "JobZoneDefinitions");
        }
    }
}
