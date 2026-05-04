using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionnairesSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPartnerDepartmentId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentId",
                table: "Partners",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_DepartmentId",
                table: "Partners",
                column: "DepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Partners_Departments_DepartmentId",
                table: "Partners",
                column: "DepartmentId",
                principalTable: "Departments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partners_Departments_DepartmentId",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_Partners_DepartmentId",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "DepartmentId",
                table: "Partners");
        }
    }
}
