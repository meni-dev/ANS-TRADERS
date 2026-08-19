using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReorderLevel",
                table: "products",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StockOnHand",
                table: "products",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "stock_movements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MovementType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    MovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_movements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_movements_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_MovedAt",
                table: "stock_movements",
                column: "MovedAt");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ProductId_MovedAt",
                table: "stock_movements",
                columns: new[] { "ProductId", "MovedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ReferenceId",
                table: "stock_movements",
                column: "ReferenceId");

            // Backfill. Products and documents already existed before stock was tracked, so the
            // ledger is rebuilt from them rather than starting empty and leaving every quantity
            // unexplained. Opening balances and every non-cancelled purchase and invoice line are
            // replayed in the order they were created, carrying a running balance.
            //
            // Cancelled documents are skipped outright rather than written as a movement plus its
            // reversal: the net effect is identical and the ledger stays readable.
            migrationBuilder.Sql("""
                INSERT INTO stock_movements
                    ("Id", "ProductId", "PartNumber", "ItemName", "MovementType", "Quantity",
                     "BalanceAfter", "MovedAt", "ReferenceId", "ReferenceNumber", "Notes")
                SELECT
                    gen_random_uuid(),
                    m.product_id,
                    p."PartNumber",
                    p."ItemName",
                    m.movement_type,
                    m.quantity,
                    SUM(m.quantity) OVER (
                        PARTITION BY m.product_id
                        ORDER BY m.moved_at, m.sort_key
                        ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW),
                    m.moved_at,
                    m.reference_id,
                    m.reference_number,
                    m.notes
                FROM (
                    SELECT
                        p."Id"                  AS product_id,
                        'Opening'::varchar      AS movement_type,
                        p."OpeningStock"        AS quantity,
                        p."CreatedAt"           AS moved_at,
                        NULL::uuid              AS reference_id,
                        NULL::varchar           AS reference_number,
                        'Opening stock'::varchar AS notes,
                        0                       AS sort_key
                    FROM products p
                    WHERE p."OpeningStock" <> 0

                    UNION ALL

                    SELECT
                        pi."ProductId",
                        'Purchase'::varchar,
                        pi."Quantity",
                        pr."CreatedAt",
                        pr."Id",
                        pr."PurchaseNumber"::varchar,
                        NULL::varchar,
                        1
                    FROM purchase_items pi
                    JOIN purchases pr ON pr."Id" = pi."PurchaseId"
                    WHERE pr."Status" <> 'Cancelled'

                    UNION ALL

                    SELECT
                        ii."ProductId",
                        'Sale'::varchar,
                        -ii."Quantity",
                        inv."CreatedAt",
                        inv."Id",
                        inv."InvoiceNumber"::varchar,
                        NULL::varchar,
                        1
                    FROM invoice_items ii
                    JOIN invoices inv ON inv."Id" = ii."InvoiceId"
                    WHERE inv."Status" <> 'Cancelled'
                ) m
                JOIN products p ON p."Id" = m.product_id;
                """);

            // Stock on hand is, by definition, the sum of the ledger. Deriving it here rather than
            // copying OpeningStock means the two can never start out disagreeing.
            migrationBuilder.Sql("""
                UPDATE products p
                SET "StockOnHand" = COALESCE(
                    (SELECT SUM(m."Quantity") FROM stock_movements m WHERE m."ProductId" = p."Id"), 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_movements");

            migrationBuilder.DropColumn(
                name: "ReorderLevel",
                table: "products");

            migrationBuilder.DropColumn(
                name: "StockOnHand",
                table: "products");
        }
    }
}
