using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjustmentReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable, and existing adjustments keep a null code: what those corrections were for
            // lives in their free-text note and nowhere else, and picking a code for them now would
            // be inventing history. The loss report counts what it can and ignores the rest.
            migrationBuilder.AddColumn<int>(
                name: "AdjustmentReason",
                table: "stock_movements",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustmentReason",
                table: "stock_movements");
        }
    }
}
