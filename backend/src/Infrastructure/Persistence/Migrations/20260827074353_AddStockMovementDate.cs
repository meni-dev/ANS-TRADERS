using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Gives every stock movement the date of the document that caused it.
    /// <para>
    /// Hand-written because the scaffolded version defaulted every existing row to 0001-01-01. A
    /// default of today would have been worse: it would have collapsed weeks of history onto one
    /// day and made the stock register agree with nothing. The rows already know their own date —
    /// it is sitting on the invoice, purchase or note behind <c>ReferenceId</c>.
    /// </para>
    /// <para>
    /// Adjustments and opening stock have no document, so they fall back to the day the row was
    /// written, read in the shop's timezone rather than UTC — the whole point of the column is that
    /// it answers in shop days.
    /// </para>
    /// </summary>
    public partial class AddStockMovementDate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "MovementDate",
                table: "stock_movements",
                type: "date",
                nullable: true);

            // Written as four scoped updates rather than one join: a movement points at exactly one
            // kind of document, and separate statements say so plainly instead of leaning on a
            // chain of outer joins that all have to miss.
            migrationBuilder.Sql("""
                UPDATE stock_movements sm SET "MovementDate" = i."InvoiceDate"
                FROM invoices i WHERE i."Id" = sm."ReferenceId";
                """);
            migrationBuilder.Sql("""
                UPDATE stock_movements sm SET "MovementDate" = p."InvoiceDate"
                FROM purchases p WHERE p."Id" = sm."ReferenceId";
                """);
            migrationBuilder.Sql("""
                UPDATE stock_movements sm SET "MovementDate" = cn."NoteDate"
                FROM credit_notes cn WHERE cn."Id" = sm."ReferenceId";
                """);
            migrationBuilder.Sql("""
                UPDATE stock_movements sm SET "MovementDate" = dn."NoteDate"
                FROM debit_notes dn WHERE dn."Id" = sm."ReferenceId";
                """);

            // Adjustments, opening stock, and anything whose document has since gone.
            migrationBuilder.Sql("""
                UPDATE stock_movements
                SET "MovementDate" = ("MovedAt" AT TIME ZONE 'Asia/Kolkata')::date
                WHERE "MovementDate" IS NULL;
                """);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "MovementDate",
                table: "stock_movements",
                type: "date",
                nullable: false);

            // The register asks for one product over a date range, in that order.
            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_ProductId_MovementDate",
                table: "stock_movements",
                columns: new[] { "ProductId", "MovementDate" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_MovementDate",
                table: "stock_movements",
                column: "MovementDate");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_stock_movements_MovementDate", table: "stock_movements");
            migrationBuilder.DropIndex(name: "IX_stock_movements_ProductId_MovementDate", table: "stock_movements");
            migrationBuilder.DropColumn(name: "MovementDate", table: "stock_movements");
        }
    }
}
