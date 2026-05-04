using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuestionnairesSystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NotificationBilingualAndParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Title",
                table: "InboxNotifications",
                newName: "TitleEn");

            migrationBuilder.RenameColumn(
                name: "Message",
                table: "InboxNotifications",
                newName: "MessageEn");

            migrationBuilder.AddColumn<string>(
                name: "MessageAr",
                table: "InboxNotifications",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RelatedEntityParentId",
                table: "InboxNotifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TitleAr",
                table: "InboxNotifications",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE InboxNotifications SET TitleAr = TitleEn WHERE TitleAr = N'' OR TitleAr IS NULL;");
            migrationBuilder.Sql(
                "UPDATE InboxNotifications SET MessageAr = MessageEn WHERE MessageAr = N'' OR MessageAr IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MessageAr",
                table: "InboxNotifications");

            migrationBuilder.DropColumn(
                name: "RelatedEntityParentId",
                table: "InboxNotifications");

            migrationBuilder.DropColumn(
                name: "TitleAr",
                table: "InboxNotifications");

            migrationBuilder.RenameColumn(
                name: "TitleEn",
                table: "InboxNotifications",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "MessageEn",
                table: "InboxNotifications",
                newName: "Message");
        }
    }
}
