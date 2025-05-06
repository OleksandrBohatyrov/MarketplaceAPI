using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MarketplaceAPI.Migrations
{
    /// <inheritdoc />
    public partial class ChangeSellerIdToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Products",
                type: "longtext",
                nullable: false,
                defaultValue: "Available")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
        name: "SellerId",
        table: "Products",
        type: "nvarchar(450)",
        nullable: false,
        oldClrType: typeof(int),
        oldType: "int");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Products");

            migrationBuilder.AlterColumn<int>(
        name: "SellerId",
        table: "Products",
        type: "int",
        nullable: false,
        oldClrType: typeof(string),
        oldType: "nvarchar(450)");
        }
    }
}
