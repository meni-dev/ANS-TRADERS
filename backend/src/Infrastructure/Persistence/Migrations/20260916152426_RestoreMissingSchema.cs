using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RestoreMissingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedSignInCount",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockedOutUntil",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BooksStartFrom",
                table: "shop_settings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SupplyType",
                table: "products",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SupplyType",
                table: "invoice_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "CashOtherIn",
                table: "day_closes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CashOtherOut",
                table: "day_closes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "SupplyType",
                table: "credit_note_items",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "document_counters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LastNumber = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_counters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "money_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AffectsCash = table.Column<bool>(type: "boolean", nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_money_movements", x => x.Id);
                    table.CheckConstraint("CK_money_movements_amount_positive", "\"Amount\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_document_counters_Kind_FinancialYear",
                table: "document_counters",
                columns: new[] { "Kind", "FinancialYear" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_money_movements_MovementDate",
                table: "money_movements",
                column: "MovementDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_counters");

            migrationBuilder.DropTable(
                name: "money_movements");

            migrationBuilder.DropColumn(
                name: "FailedSignInCount",
                table: "users");

            migrationBuilder.DropColumn(
                name: "LockedOutUntil",
                table: "users");

            migrationBuilder.DropColumn(
                name: "BooksStartFrom",
                table: "shop_settings");

            migrationBuilder.DropColumn(
                name: "SupplyType",
                table: "products");

            migrationBuilder.DropColumn(
                name: "SupplyType",
                table: "invoice_items");

            migrationBuilder.DropColumn(
                name: "CashOtherIn",
                table: "day_closes");

            migrationBuilder.DropColumn(
                name: "CashOtherOut",
                table: "day_closes");

            migrationBuilder.DropColumn(
                name: "SupplyType",
                table: "credit_note_items");
        }
    }
}
