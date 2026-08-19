using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Brand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VehicleBrand = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    VehicleModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AlternatePartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Hsn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    GstRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Uqc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PurchaseRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SellingRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Mrp = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MinimumStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReorderLevel = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RackLocation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OpeningStock = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    OpeningRate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_Barcode",
                table: "products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_products_Brand",
                table: "products",
                column: "Brand");

            migrationBuilder.CreateIndex(
                name: "IX_products_ItemName",
                table: "products",
                column: "ItemName");

            migrationBuilder.CreateIndex(
                name: "IX_products_PartNumber",
                table: "products",
                column: "PartNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_VehicleBrand_VehicleModel",
                table: "products",
                columns: new[] { "VehicleBrand", "VehicleModel" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "products");
        }
    }
}
