using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseAndInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CustomerPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CustomerGstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    CustomerStateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    IsInterState = table.Column<bool>(type: "boolean", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    BalanceDue = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoices_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    SupplierInvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: false),
                    SupplierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupplierGstin = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: true),
                    SupplierStateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    IsInterState = table.Column<bool>(type: "boolean", nullable: false),
                    ItemCount = table.Column<int>(type: "integer", nullable: false),
                    SubTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    IgstAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalTax = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    RoundOff = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    PaymentMode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchases_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_invoice_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_items_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_purchase_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_items_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_items_purchases_PurchaseId",
                        column: x => x.PurchaseId,
                        principalTable: "purchases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_InvoiceId",
                table: "invoice_items",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_items_ProductId",
                table: "invoice_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_BalanceDue",
                table: "invoices",
                column: "BalanceDue",
                filter: "\"BalanceDue\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_CustomerId",
                table: "invoices",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_FinancialYear_Sequence",
                table: "invoices",
                columns: new[] { "FinancialYear", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_InvoiceDate",
                table: "invoices",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_InvoiceNumber",
                table: "invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_Status",
                table: "invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_items_ProductId",
                table: "purchase_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_items_PurchaseId",
                table: "purchase_items",
                column: "PurchaseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_FinancialYear_Sequence",
                table: "purchases",
                columns: new[] { "FinancialYear", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchases_InvoiceDate",
                table: "purchases",
                column: "InvoiceDate");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_PurchaseNumber",
                table: "purchases",
                column: "PurchaseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchases_Status",
                table: "purchases",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_purchases_SupplierId_SupplierInvoiceNumber",
                table: "purchases",
                columns: new[] { "SupplierId", "SupplierInvoiceNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_items");

            migrationBuilder.DropTable(
                name: "purchase_items");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "purchases");
        }
    }
}
