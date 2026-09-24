using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceEmojiWithImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Emoji",
                table: "MenuItems");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "MenuItems",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "MenuItems");

            migrationBuilder.AddColumn<string>(
                name: "Emoji",
                table: "MenuItems",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
