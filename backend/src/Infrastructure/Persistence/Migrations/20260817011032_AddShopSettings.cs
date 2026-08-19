using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShopSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shop_settings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LegalName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Gstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AddressLine1 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    AddressLine2 = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Pincode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InvoiceFooter = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BankDetails = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    InvoiceTerms = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    InvoiceTemplate = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shop_settings", x => x.Id);
                });

            // Seeded with what the Business section of appsettings.json held, so a shop that was
            // already billing keeps the exact seller header its customers have seen. The values are
            // literals rather than a config read: a migration must produce the same database
            // whenever it runs, and configuration is free to change underneath it.
            //
            // The id is fixed — ShopSettings.SingletonId — so the row can be found without a scan
            // and can never be duplicated.
            migrationBuilder.Sql("""
                INSERT INTO shop_settings
                    ("Id", "Name", "LegalName", "Gstin", "StateCode", "State",
                     "AddressLine1", "AddressLine2", "City", "Pincode", "Phone", "Email",
                     "InvoiceFooter", "BankDetails", "InvoiceTerms", "InvoiceTemplate",
                     "CreatedAt", "UpdatedAt")
                VALUES (
                    '5e771a6c-0000-4000-8000-000000000001',
                    'ANS Traders',
                    'ANS Traders',
                    '33AAECS1234F1Z8',
                    '33',
                    'Tamil Nadu',
                    'Two-Wheeler Spare Parts',
                    NULL,
                    'Chennai',
                    '600001',
                    NULL,
                    NULL,
                    'Goods once sold will not be taken back. Subject to Chennai jurisdiction.',
                    NULL,
                    NULL,
                    'Classic',
                    now(),
                    now());
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shop_settings");
        }
    }
}
