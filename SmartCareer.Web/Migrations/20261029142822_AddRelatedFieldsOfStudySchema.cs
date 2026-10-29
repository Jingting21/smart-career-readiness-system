using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCareer.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedFieldsOfStudySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DegreeFields",
                columns: table => new
                {
                    DegreeFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CanonicalName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ClassificationSystem = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ClassificationCode = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ClassificationVersion = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DegreeFields", x => x.DegreeFieldId);
                });

            migrationBuilder.CreateTable(
                name: "RelatedFieldDatasetSources",
                columns: table => new
                {
                    RelatedFieldDatasetSourceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DatasetVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    MappingFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RetrievedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceMetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RelatedFieldDatasetSources", x => x.RelatedFieldDatasetSourceId);
                });

            migrationBuilder.CreateTable(
                name: "CareerDegreeFields",
                columns: table => new
                {
                    CareerDegreeFieldId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    DegreeFieldId = table.Column<int>(type: "int", nullable: false),
                    RelatedFieldDatasetSourceId = table.Column<int>(type: "int", nullable: false),
                    RelationshipType = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    ConfidenceLevel = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    ReviewStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    EvidenceSummary = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OriginalSocCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    OriginalCipCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    MalaysianNecCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false),
                    SourceUrls = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SourceVersions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ReviewDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReviewedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProfessionalRegulationFlag = table.Column<bool>(type: "bit", nullable: false),
                    ReviewerNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    RevalidationDueOn = table.Column<DateOnly>(type: "date", nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerDegreeFields", x => x.CareerDegreeFieldId);
                    table.CheckConstraint("CK_CareerDegreeFields_ConfidenceLevel", "[ConfidenceLevel] IN ('High', 'Medium', 'Low')");
                    table.CheckConstraint("CK_CareerDegreeFields_Publication", "([ReviewStatus] = 'Approved' AND [ReviewedBy] IS NOT NULL AND [ApprovedAt] IS NOT NULL) OR ([ReviewStatus] <> 'Approved' AND [IsPublished] = 0 AND [ApprovedAt] IS NULL)");
                    table.CheckConstraint("CK_CareerDegreeFields_RelationshipType", "[RelationshipType] IN ('Primary', 'Related', 'Alternative', 'Specialised')");
                    table.CheckConstraint("CK_CareerDegreeFields_ReviewStatus", "[ReviewStatus] IN ('Draft', 'In Review', 'Approved', 'Rejected', 'Needs Revalidation')");
                    table.ForeignKey(
                        name: "FK_CareerDegreeFields_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "CareerId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerDegreeFields_DegreeFields_DegreeFieldId",
                        column: x => x.DegreeFieldId,
                        principalTable: "DegreeFields",
                        principalColumn: "DegreeFieldId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerDegreeFields_RelatedFieldDatasetSources_RelatedFieldDatasetSourceId",
                        column: x => x.RelatedFieldDatasetSourceId,
                        principalTable: "RelatedFieldDatasetSources",
                        principalColumn: "RelatedFieldDatasetSourceId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerDegreeFields_CareerId_DegreeFieldId",
                table: "CareerDegreeFields",
                columns: new[] { "CareerId", "DegreeFieldId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerDegreeFields_DegreeFieldId",
                table: "CareerDegreeFields",
                column: "DegreeFieldId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerDegreeFields_RelatedFieldDatasetSourceId",
                table: "CareerDegreeFields",
                column: "RelatedFieldDatasetSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerDegreeFields_ReviewStatus_IsPublished",
                table: "CareerDegreeFields",
                columns: new[] { "ReviewStatus", "IsPublished" });

            migrationBuilder.CreateIndex(
                name: "IX_DegreeFields_ClassificationSystem_ClassificationCode_ClassificationVersion",
                table: "DegreeFields",
                columns: new[] { "ClassificationSystem", "ClassificationCode", "ClassificationVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RelatedFieldDatasetSources_DatasetVersion",
                table: "RelatedFieldDatasetSources",
                column: "DatasetVersion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerDegreeFields");

            migrationBuilder.DropTable(
                name: "DegreeFields");

            migrationBuilder.DropTable(
                name: "RelatedFieldDatasetSources");
        }
    }
}
