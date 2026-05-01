using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionnairesSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAvatarFileName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AvatarFileName",
                table: "Users",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarFileName",
                table: "Users");
        }
    }
}
