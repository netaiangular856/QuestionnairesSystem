using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionnairesSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SurveyScheduleOpensCloses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ClosesAtUtc",
                table: "Surveys",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OpensAtUtc",
                table: "Surveys",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_Status_ClosesAtUtc",
                table: "Surveys",
                columns: new[] { "Status", "ClosesAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Surveys_Status_ClosesAtUtc",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "ClosesAtUtc",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "OpensAtUtc",
                table: "Surveys");
        }
    }
}
