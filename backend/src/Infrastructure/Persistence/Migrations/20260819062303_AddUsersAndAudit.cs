using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsersAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "suppliers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "suppliers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "BooksLockedUpTo",
                table: "shop_settings",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "shop_settings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "shop_settings",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "purchases",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "purchases",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "products",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "invoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "expenses",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "expenses",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "debit_notes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "debit_notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "day_closes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "day_closes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "customers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "credit_notes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "credit_notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedByName",
                table: "cheque_details",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByUserId",
                table: "cheque_details",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityLabel = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    Detail = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Username = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                    LastSignedInAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_sessions_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_Action",
                table: "audit_events",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_audit_events_OccurredAt",
                table: "audit_events",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_Token",
                table: "user_sessions",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_sessions_UserId",
                table: "user_sessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "suppliers");

            migrationBuilder.DropColumn(
                name: "BooksLockedUpTo",
                table: "shop_settings");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "shop_settings");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "shop_settings");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "purchases");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "products");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "expenses");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "debit_notes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "debit_notes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "day_closes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "day_closes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "customers");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "credit_notes");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "credit_notes");

            migrationBuilder.DropColumn(
                name: "CreatedByName",
                table: "cheque_details");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "cheque_details");
        }
    }
}
