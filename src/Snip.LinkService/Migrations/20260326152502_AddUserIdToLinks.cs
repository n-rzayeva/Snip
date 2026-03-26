using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Snip.LinkService.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Links",
                type: "character varying(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Links_UserId",
                table: "Links",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Links_UserId",
                table: "Links");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Links");
        }
    }
}
