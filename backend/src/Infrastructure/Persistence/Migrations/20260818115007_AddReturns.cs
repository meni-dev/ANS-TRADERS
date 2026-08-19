using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No data fix-up here, unlike 20260818015213 which needed an UPDATE before its
            // constraint would hold. Every new column defaults to 0, and a live row already
            // satisfies "BalanceDue = GrandTotal - AmountPaid - 0" — verified against the data
            // before this was generated. The columns are added before the constraints below, so
            // the order is what makes that true rather than luck.
            migrationBuilder.DropCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payment_allocations_single_document",
                table: "payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices");

            migrationBuilder.AddColumn<decimal>(
                name: "DebitAppliedAmount",
                table: "purchases",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "purchase_items",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "CreditNoteId",
                table: "payment_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DebitNoteId",
                table: "payment_allocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CreditAppliedAmount",
                table: "invoices",
                type: "numeric(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "invoice_items",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "credit_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    NoteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CustomerGstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    CustomerStateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    IsInterState = table.Column<bool>(type: "boolean", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AppliedToInvoiceAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_notes", x => x.Id);
                    table.CheckConstraint("CK_credit_notes_applied_within_total", "\"AppliedToInvoiceAmount\" >= 0 AND \"AppliedToInvoiceAmount\" <= \"GrandTotal\"");
                    table.CheckConstraint("CK_credit_notes_refund_within_credit", "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"GrandTotal\" - \"AppliedToInvoiceAmount\"");
                    table.ForeignKey(
                        name: "FK_credit_notes_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_credit_notes_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "debit_notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DebitNoteNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    NoteDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupplierGstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    SupplierStateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    IsInterState = table.Column<bool>(type: "boolean", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AppliedToPurchaseAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RefundedAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debit_notes", x => x.Id);
                    table.CheckConstraint("CK_debit_notes_applied_within_total", "\"AppliedToPurchaseAmount\" >= 0 AND \"AppliedToPurchaseAmount\" <= \"GrandTotal\"");
                    table.CheckConstraint("CK_debit_notes_refund_within_credit", "\"RefundedAmount\" >= 0 AND \"RefundedAmount\" <= \"GrandTotal\" - \"AppliedToPurchaseAmount\"");
                    table.ForeignKey(
                        name: "FK_debit_notes_purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_debit_notes_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "credit_note_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hsn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Uqc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GstRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_credit_note_items", x => x.Id);
                    table.CheckConstraint("CK_credit_note_items_quantity_positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_credit_note_items_credit_notes_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalTable: "credit_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_credit_note_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "debit_note_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DebitNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ItemName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Hsn = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Uqc = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountPercent = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GstRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_debit_note_items", x => x.Id);
                    table.CheckConstraint("CK_debit_note_items_quantity_positive", "\"Quantity\" > 0");
                    table.ForeignKey(
                        name: "FK_debit_note_items_debit_notes_DebitNoteId",
                        column: x => x.DebitNoteId,
                        principalTable: "debit_notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_debit_note_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases",
                sql: "CASE WHEN \"Status\" = 'Cancelled' THEN \"BalanceDue\" = 0 ELSE \"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\" - \"DebitAppliedAmount\" END");

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchases_debit_applied",
                table: "purchases",
                sql: "\"DebitAppliedAmount\" >= 0 AND \"DebitAppliedAmount\" <= \"GrandTotal\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchase_items_returned_quantity",
                table: "purchase_items",
                sql: "\"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\"");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_CreditNoteId",
                table: "payment_allocations",
                column: "CreditNoteId",
                filter: "\"IsReversed\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_payment_allocations_DebitNoteId",
                table: "payment_allocations",
                column: "DebitNoteId",
                filter: "\"IsReversed\" = false");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payment_allocations_single_document",
                table: "payment_allocations",
                sql: "num_nonnulls(\"InvoiceId\", \"PurchaseId\", \"CreditNoteId\", \"DebitNoteId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices",
                sql: "CASE WHEN \"Status\" = 'Cancelled' THEN \"BalanceDue\" = 0 ELSE \"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\" - \"CreditAppliedAmount\" END");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invoices_credit_applied",
                table: "invoices",
                sql: "\"CreditAppliedAmount\" >= 0 AND \"CreditAppliedAmount\" <= \"GrandTotal\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invoice_items_returned_quantity",
                table: "invoice_items",
                sql: "\"ReturnedQuantity\" >= 0 AND \"ReturnedQuantity\" <= \"Quantity\"");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_items_CreditNoteId",
                table: "credit_note_items",
                column: "CreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_items_InvoiceItemId",
                table: "credit_note_items",
                column: "InvoiceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_note_items_ProductId",
                table: "credit_note_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_CreditNoteNumber",
                table: "credit_notes",
                column: "CreditNoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_CustomerId",
                table: "credit_notes",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_FinancialYear_Sequence",
                table: "credit_notes",
                columns: new[] { "FinancialYear", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_InvoiceId",
                table: "credit_notes",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_NoteDate",
                table: "credit_notes",
                column: "NoteDate");

            migrationBuilder.CreateIndex(
                name: "IX_credit_notes_Status",
                table: "credit_notes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_debit_note_items_DebitNoteId",
                table: "debit_note_items",
                column: "DebitNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_debit_note_items_ProductId",
                table: "debit_note_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_debit_note_items_PurchaseItemId",
                table: "debit_note_items",
                column: "PurchaseItemId");

            migrationBuilder.CreateIndex(
                name: "IX_debit_notes_DebitNoteNumber",
                table: "debit_notes",
                column: "DebitNoteNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_debit_notes_FinancialYear_Sequence",
                table: "debit_notes",
                columns: new[] { "FinancialYear", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_debit_notes_NoteDate",
                table: "debit_notes",
                column: "NoteDate");

            migrationBuilder.CreateIndex(
                name: "IX_debit_notes_PurchaseId",
                table: "debit_notes",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_debit_notes_Status",
                table: "debit_notes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_debit_notes_SupplierId",
                table: "debit_notes",
                column: "SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_payment_allocations_credit_notes_CreditNoteId",
                table: "payment_allocations",
                column: "CreditNoteId",
                principalTable: "credit_notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_payment_allocations_debit_notes_DebitNoteId",
                table: "payment_allocations",
                column: "DebitNoteId",
                principalTable: "debit_notes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_payment_allocations_credit_notes_CreditNoteId",
                table: "payment_allocations");

            migrationBuilder.DropForeignKey(
                name: "FK_payment_allocations_debit_notes_DebitNoteId",
                table: "payment_allocations");

            migrationBuilder.DropTable(
                name: "credit_note_items");

            migrationBuilder.DropTable(
                name: "debit_note_items");

            migrationBuilder.DropTable(
                name: "credit_notes");

            migrationBuilder.DropTable(
                name: "debit_notes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchases_debit_applied",
                table: "purchases");

            migrationBuilder.DropCheckConstraint(
                name: "CK_purchase_items_returned_quantity",
                table: "purchase_items");

            migrationBuilder.DropIndex(
                name: "IX_payment_allocations_CreditNoteId",
                table: "payment_allocations");

            migrationBuilder.DropIndex(
                name: "IX_payment_allocations_DebitNoteId",
                table: "payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_payment_allocations_single_document",
                table: "payment_allocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invoices_credit_applied",
                table: "invoices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_invoice_items_returned_quantity",
                table: "invoice_items");

            migrationBuilder.DropColumn(
                name: "DebitAppliedAmount",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "purchase_items");

            migrationBuilder.DropColumn(
                name: "CreditNoteId",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "DebitNoteId",
                table: "payment_allocations");

            migrationBuilder.DropColumn(
                name: "CreditAppliedAmount",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "invoice_items");

            migrationBuilder.AddCheckConstraint(
                name: "CK_purchases_balance_due",
                table: "purchases",
                sql: "CASE WHEN \"Status\" = 'Cancelled' THEN \"BalanceDue\" = 0 ELSE \"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\" END");

            migrationBuilder.AddCheckConstraint(
                name: "CK_payment_allocations_single_document",
                table: "payment_allocations",
                sql: "num_nonnulls(\"InvoiceId\", \"PurchaseId\") = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_invoices_balance_due",
                table: "invoices",
                sql: "CASE WHEN \"Status\" = 'Cancelled' THEN \"BalanceDue\" = 0 ELSE \"BalanceDue\" = \"GrandTotal\" - \"AmountPaid\" END");
        }
    }
}
