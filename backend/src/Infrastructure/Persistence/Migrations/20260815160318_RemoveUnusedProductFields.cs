using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnusedProductFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_Barcode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "AlternatePartNumber",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "products");

            migrationBuilder.DropColumn(
                name: "MinimumStock",
                table: "products");

            migrationBuilder.DropColumn(
                name: "OpeningRate",
                table: "products");

            migrationBuilder.DropColumn(
                name: "RackLocation",
                table: "products");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternatePartNumber",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MinimumStock",
                table: "products",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningRate",
                table: "products",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "RackLocation",
                table: "products",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ReorderLevel",
                table: "products",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_products_Barcode",
                table: "products",
                column: "Barcode");
        }
    }
}
