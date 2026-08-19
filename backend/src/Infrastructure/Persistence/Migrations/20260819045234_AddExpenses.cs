using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExpenseNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    FinancialYear = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ReferenceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaidTo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_expenses", x => x.Id);
                    table.CheckConstraint("CK_expenses_amount_positive", "\"Amount\" > 0");
                });

            migrationBuilder.CreateIndex(
                name: "IX_expenses_Category",
                table: "expenses",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_ExpenseDate",
                table: "expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_expenses_ExpenseNumber",
                table: "expenses",
                column: "ExpenseNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_expenses_FinancialYear_Sequence",
                table: "expenses",
                columns: new[] { "FinancialYear", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expenses");
        }
    }
}
