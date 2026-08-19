using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductClassificationAndGstSplit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_Brand",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "products");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "products");

            migrationBuilder.AlterColumn<string>(
                name: "Hsn",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CgstRate",
                table: "products",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SgstRate",
                table: "products",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            // Rows created before these columns existed would otherwise sit at 0 while their
            // GstRate says otherwise. Backfill them with the same even split the service applies.
            migrationBuilder.Sql(
                @"UPDATE products
                     SET ""CgstRate"" = ROUND(""GstRate"" / 2, 2),
                         ""SgstRate"" = ROUND(""GstRate"" / 2, 2);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CgstRate",
                table: "products");

            migrationBuilder.DropColumn(
                name: "SgstRate",
                table: "products");

            migrationBuilder.AlterColumn<string>(
                name: "Hsn",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_Brand",
                table: "products",
                column: "Brand");
        }
    }
}
