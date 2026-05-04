using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionnairesSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyPublicPortalArticleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PublicArticleBodyAr",
                table: "Surveys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicArticleBodyEn",
                table: "Surveys",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PublicArticleEnabled",
                table: "Surveys",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PublicArticleTitleAr",
                table: "Surveys",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicArticleTitleEn",
                table: "Surveys",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnPublicPortal",
                table: "Surveys",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublicArticleBodyAr",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "PublicArticleBodyEn",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "PublicArticleEnabled",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "PublicArticleTitleAr",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "PublicArticleTitleEn",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "ShowOnPublicPortal",
                table: "Surveys");
        }
    }
}
