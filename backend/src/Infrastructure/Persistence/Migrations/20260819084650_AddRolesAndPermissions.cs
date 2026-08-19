using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedByName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Permission = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });


            // ---------------------------------------------------------------- the two starting roles
            //
            // Seeded here rather than from application code because the column below is about to be
            // made NOT NULL against a foreign key: the rows every existing account will point at have
            // to exist inside the same transaction as the schema that requires them.
            //
            // Fixed ids, not generated ones, so a shop that restores a backup and a shop that starts
            // fresh end up with the same two roles rather than two sets that look alike.
            migrationBuilder.Sql(@"
                INSERT INTO roles (""Id"", ""Name"", ""Description"", ""IsSystem"", ""CreatedAt"", ""UpdatedAt"")
                VALUES
                    ('8f5b1d2e-0000-4000-8000-000000000001', 'Owner',
                     'Everything. The built-in role — it cannot be edited or deleted.', true, now(), now()),
                    ('8f5b1d2e-0000-4000-8000-000000000002', 'Counter Staff',
                     'Sells over the counter and takes money. Cannot see cost, cancel, or correct stock.', false, now(), now());
            ");

            migrationBuilder.Sql(@"
                INSERT INTO role_permissions (""Id"", ""RoleId"", ""Permission"") VALUES
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'BillCreate'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'BillCancel'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'SalesReturn'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'BillDiscount'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'PurchaseView'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'PurchaseCreate'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'PurchaseCancel'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'PurchaseReturn'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'StockView'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'StockAdjust'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'ProductManage'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'PaymentRecord'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'PaymentCancel'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'ExpenseRecord'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'CashDayClose'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'CostView'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'ReportView'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'UserManage'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'SettingsEdit'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'BooksLock'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000001', 'AuditView');
            ");

            migrationBuilder.Sql(@"
                INSERT INTO role_permissions (""Id"", ""RoleId"", ""Permission"") VALUES
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000002', 'BillCreate'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000002', 'SalesReturn'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000002', 'StockView'),
                    (gen_random_uuid(), '8f5b1d2e-0000-4000-8000-000000000002', 'PaymentRecord');
            ");

            // ------------------------------------------------------- move the existing accounts across
            //
            // Nullable first, filled from the old column, and only then made required. Adding it as
            // NOT NULL with a zero default — which is what the scaffolder wrote — would point every
            // existing account at a role that does not exist and fail the foreign key.
            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "users",
                type: "uuid",
                nullable: true);

            // Anything that is not explicitly Owner becomes Counter Staff. Erring towards less is the
            // only safe direction: a person who finds a screen missing says so within the hour, while
            // one quietly handed the cancel button says nothing at all.
            migrationBuilder.Sql(@"
                UPDATE users
                SET ""RoleId"" = CASE WHEN ""Role"" = 'Owner'
                    THEN '8f5b1d2e-0000-4000-8000-000000000001'::uuid
                    ELSE '8f5b1d2e-0000-4000-8000-000000000002'::uuid
                END;
            ");

            migrationBuilder.AlterColumn<Guid>(
                name: "RoleId",
                table: "users",
                type: "uuid",
                nullable: false);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "IX_users_RoleId",
                table: "users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_RoleId_Permission",
                table: "role_permissions",
                columns: new[] { "RoleId", "Permission" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                table: "roles",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users",
                column: "RoleId",
                principalTable: "roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_users_roles_RoleId",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_RoleId",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Staff");

            // Read back before the tables go: anyone holding the built-in role was an Owner, and
            // everybody else lands on Staff — the same two buckets this migration started from.
            migrationBuilder.Sql(@"
                UPDATE users u
                SET ""Role"" = CASE WHEN r.""IsSystem"" THEN 'Owner' ELSE 'Staff' END
                FROM roles r
                WHERE r.""Id"" = u.""RoleId"";
            ");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "users");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}
