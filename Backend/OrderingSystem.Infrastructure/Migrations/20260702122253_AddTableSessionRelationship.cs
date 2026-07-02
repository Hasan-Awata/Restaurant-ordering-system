using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableSessionRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Floor",
                table: "Tables",
                newName: "FloorNumber");

            migrationBuilder.AlterColumn<string>(
                name: "QrCode",
                table: "Tables",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "FloorNumber",
                table: "Tables",
                newName: "Floor");

            migrationBuilder.AlterColumn<string>(
                name: "QrCode",
                table: "Tables",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }
    }
}
