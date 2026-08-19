using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BalanceDueZeroOnCancelledDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices");

            // The rule being added is new, so existing rows have never had to satisfy it: two
            // cancelled documents still carry a balance today. Only BalanceDue is corrected —
            // AmountPaid is left standing, because it is the sole surviving record that money was
            // taken, and the backfill reads it to reconstruct the payment behind each one.
            migrationBuilder.Sql("""
                UPDATE invoices SET "BalanceDue" = 0 WHERE "Status" = 'Cancelled';
                UPDATE purchases SET "BalanceDue" = 0 WHERE "Status" = 'Cancelled';
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases",
                sql: "CASE WHEN \"Status\" = 'Cancelled' THEN \"BalanceDue\" = 0 ELSE \"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\" END");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices",
                sql: "CASE WHEN \"Status\" = 'Cancelled' THEN \"BalanceDue\" = 0 ELSE \"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\" END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices");

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases",
                sql: "\"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices",
                sql: "\"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\"");
        }
    }
}
