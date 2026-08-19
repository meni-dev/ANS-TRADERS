using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingBalance",
                table: "suppliers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "suppliers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<decimal>(
                name: "BalanceDue",
                table: "purchases",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "purchases",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "products",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateOnly>(
                name: "DueDate",
                table: "invoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "invoices",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "CreditDays",
                table: "customers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OutstandingBalance",
                table: "customers",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "customers",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.CreateTable(
                name: "party_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntryType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BalanceAfter = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    EntryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_ledger_entries", x => x.Id);
                    table.CheckConstraint("CK_party_ledger_entries_single_party", "num_nonnulls(\"CustomerId\", \"SupplierId\") = 1");
                    table.ForeignKey(
                        name: "FK_party_ledger_entries_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_party_ledger_entries_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    FinancialYear = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: true),
                    Direction = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    PartyName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    UnallocatedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsCounterPayment = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.CheckConstraint("CK_payments_allocation_adds_up", "\"AllocatedAmount\" + \"UnallocatedAmount\" = \"Amount\"");
                    table.CheckConstraint("CK_payments_amount_positive", "\"Amount\" > 0");
                    table.CheckConstraint("CK_payments_single_party", "num_nonnulls(\"CustomerId\", \"SupplierId\") <= 1");
                    table.ForeignKey(
                        name: "FK_payments_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cheque_details",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChequeNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChequeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReceivedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DepositedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    ClearedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    BouncedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    BounceReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cheque_details", x => x.PaymentId);
                    table.ForeignKey(
                        name: "FK_cheque_details_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    DocumentNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DocumentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AllocatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsReversed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_allocations", x => x.Id);
                    table.CheckConstraint("CK_payment_allocations_amount_positive", "\"Amount\" > 0");
                    table.CheckConstraint("CK_payment_allocations_single_document", "num_nonnulls(\"InvoiceId\", \"PurchaseId\") = 1");
                    table.ForeignKey(
                        name: "FK_payment_allocations_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payment_allocations_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_payment_allocations_purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_purchases_BalanceDue",
                table: "purchases",
                column: "BalanceDue",
                filter: "\"BalanceDue\" > 0");

            // The column arrives defaulted to zero, which is a lie for every bill already on the
            // books — so it is filled in before the constraint that would reject it. Purely
            // arithmetic: no status special-casing, because the Status filter is what keeps
            // cancelled documents out of queries, and the sum has to hold for every row.
            migrationBuilder.Sql("""
                UPDATE purchases SET "BalanceDue" = "GrandTotal" - "AmountPaid";
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases",
                sql: "\"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices",
                sql: "\"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\"");

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_ChequeNumber",
                table: "cheque_details",
                column: "ChequeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_cheque_details_Status_ChequeDate",
                table: "cheque_details",
                columns: new[] { "Status", "ChequeDate" });

            migrationBuilder.CreateIndex(
                name: "IX_party_ledger_entries_CustomerId_RecordedAt",
                table: "party_ledger_entries",
                columns: new[] { "CustomerId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_party_ledger_entries_EntryDate",
                table: "party_ledger_entries",
                column: "EntryDate");

            migrationBuilder.CreateIndex(
                name: "IX_party_ledger_entries_ReferenceId",
                table: "party_ledger_entries",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_party_ledger_entries_SupplierId_RecordedAt",
                table: "party_ledger_entries",
                columns: new[] { "SupplierId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_InvoiceId",
                table: "payment_allocations",
                column: "InvoiceId",
                filter: "\"IsReversed\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_PaymentId",
                table: "payment_allocations",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_PurchaseId",
                table: "payment_allocations",
                column: "PurchaseId",
                filter: "\"IsReversed\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_payments_CustomerId",
                table: "payments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Direction_FinancialYear_Sequence",
                table: "payments",
                columns: new[] { "Direction", "FinancialYear", "Sequence" },
                unique: true,
                filter: "\"Sequence\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PaymentDate",
                table: "payments",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_payments_ReceiptNumber",
                table: "payments",
                column: "ReceiptNumber",
                unique: true,
                filter: "\"ReceiptNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_Status",
                table: "payments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_payments_SupplierId",
                table: "payments",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_UnallocatedAmount",
                table: "payments",
                column: "UnallocatedAmount",
                filter: "\"UnallocatedAmount\" > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cheque_details");

            migrationBuilder.DropTable(
                name: "party_ledger_entries");

            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropIndex(
                name: "IX_purchases_BalanceDue",
                table: "purchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "OutstandingBalance",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "BalanceDue",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "products");

            migrationBuilder.DropColumn(
                name: "DueDate",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "CreditDays",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "OutstandingBalance",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "customers");
        }
    }
}
