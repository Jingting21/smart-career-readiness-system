using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartCareer.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerTasks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerTasks",
                columns: table => new
                {
                    CareerTaskId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerId = table.Column<int>(type: "int", nullable: false),
                    OnetTaskId = table.Column<int>(type: "int", nullable: false),
                    Statement = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TaskType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Importance = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Relevance = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    IncumbentsResponding = table.Column<int>(type: "int", nullable: true),
                    SourceUpdatedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    DomainSource = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceVersion = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerTasks", x => x.CareerTaskId);
                    table.ForeignKey(
                        name: "FK_CareerTasks_Careers_CareerId",
                        column: x => x.CareerId,
                        principalTable: "Careers",
                        principalColumn: "CareerId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerTasks_CareerId_OnetTaskId",
                table: "CareerTasks",
                columns: new[] { "CareerId", "OnetTaskId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerTasks_CareerId_TaskType_Importance",
                table: "CareerTasks",
                columns: new[] { "CareerId", "TaskType", "Importance" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerTasks");
        }
    }
}
