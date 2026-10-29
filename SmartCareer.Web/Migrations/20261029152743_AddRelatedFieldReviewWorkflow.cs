using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCareer.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddRelatedFieldReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ReviewRevision",
                table: "CareerDegreeFields",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CareerDegreeFieldReviewEvents",
                columns: table => new
                {
                    CareerDegreeFieldReviewEventId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerDegreeFieldId = table.Column<int>(type: "int", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EventType = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    Decision = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: true),
                    IsIndependentReview = table.Column<bool>(type: "bit", nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    EvidenceFingerprint = table.Column<string>(type: "char(64)", unicode: false, maxLength: 64, nullable: false),
                    PreviousReviewStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    NewReviewStatus = table.Column<string>(type: "varchar(30)", unicode: false, maxLength: 30, nullable: false),
                    PreviousIsPublished = table.Column<bool>(type: "bit", nullable: false),
                    NewIsPublished = table.Column<bool>(type: "bit", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerDegreeFieldReviewEvents", x => x.CareerDegreeFieldReviewEventId);
                    table.CheckConstraint("CK_CareerDegreeFieldReviewEvents_Decision", "[Decision] IS NULL OR [Decision] IN ('Approve', 'Reject', 'Needs Revalidation')");
                    table.CheckConstraint("CK_CareerDegreeFieldReviewEvents_EventType", "[EventType] IN ('ReviewSubmitted', 'Approved', 'Published', 'Unpublished')");
                    table.ForeignKey(
                        name: "FK_CareerDegreeFieldReviewEvents_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerDegreeFieldReviewEvents_CareerDegreeFields_CareerDegreeFieldId",
                        column: x => x.CareerDegreeFieldId,
                        principalTable: "CareerDegreeFields",
                        principalColumn: "CareerDegreeFieldId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerDegreeFieldReviewEvents_ActorUserId",
                table: "CareerDegreeFieldReviewEvents",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerDegreeFieldReviewEvents_CareerDegreeFieldId_EventType_EvidenceFingerprint",
                table: "CareerDegreeFieldReviewEvents",
                columns: new[] { "CareerDegreeFieldId", "EventType", "EvidenceFingerprint" });

            migrationBuilder.CreateIndex(
                name: "IX_CareerDegreeFieldReviewEvents_CareerDegreeFieldId_OccurredAt",
                table: "CareerDegreeFieldReviewEvents",
                columns: new[] { "CareerDegreeFieldId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerDegreeFieldReviewEvents");

            migrationBuilder.DropColumn(
                name: "ReviewRevision",
                table: "CareerDegreeFields");
        }
    }
}
