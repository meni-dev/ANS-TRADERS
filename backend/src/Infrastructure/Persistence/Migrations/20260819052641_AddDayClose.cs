using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDayClose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "day_closes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CloseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OpeningCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CashReceived = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CashPaidOut = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CashExpenses = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ExpectedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CountedCash = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Difference = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_day_closes", x => x.Id);
                    table.CheckConstraint("CK_day_closes_difference", "\"Difference\" = \"CountedCash\" - \"ExpectedCash\"");
                });

            migrationBuilder.CreateIndex(
                name: "IX_day_closes_CloseDate",
                table: "day_closes",
                column: "CloseDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "day_closes");
        }
    }
}
