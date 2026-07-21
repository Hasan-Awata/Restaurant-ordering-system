using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderingSystem.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PreventingRaceCondition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "TableSessions",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Tables",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Orders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "TableSessions");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Tables");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Orders");
        }
    }
}
