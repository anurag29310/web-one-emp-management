using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiTenancy : Migration
    {
        /// <summary>Every pre-existing row (there is exactly one tenant before this migration —
        /// this deployment) is assigned to this fixed-GUID default Company, seeded below.</summary>
        private static readonly Guid DefaultCompanyId = new("00000000-0000-0000-0000-000000000001");

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Create the new tenancy tables.
            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Timezone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RegisteredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SuspendedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RejectedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlatformSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IsPublicRegistrationEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    RequireApprovalForNewCompanies = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSettings", x => x.Id);
                });

            // 2) Seed the default Company that every pre-existing row will be backfilled onto.
            // Status is Active (not Trial) — this is live, already-running data, not a new signup.
            migrationBuilder.InsertData(
                table: "Companies",
                columns: new[] { "Id", "Name", "Status", "Timezone", "Currency", "RegisteredAtUtc", "ApprovedAtUtc", "IsDeleted", "CreatedAtUtc" },
                values: new object[] { DefaultCompanyId, "Default Company", "Active", "UTC", "USD", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            // 3) Seed the singleton PlatformSettings row and the SuperAdmin role.
            migrationBuilder.InsertData(
                table: "PlatformSettings",
                columns: new[] { "Id", "IsPublicRegistrationEnabled", "RequireApprovalForNewCompanies", "UpdatedAtUtc" },
                values: new object[] { new Guid("99999999-9999-9999-9999-999999999999"), true, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAtUtc", "Description", "Name", "UpdatedAtUtc" },
                values: new object[] { new Guid("55555555-5555-5555-5555-555555555555"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "SuperAdmin", null });

            // 4) Add CompanyId as nullable everywhere first — it can only be populated once the
            // column exists, and Users/AuditLogs stay nullable permanently (SuperAdmin users have no
            // company; platform-level audit events have no single tenant).
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Teams",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Shifts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "OfficeLocations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "LeaveTypes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Employees",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Designations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "Departments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "AuditLogs",
                type: "uuid",
                nullable: true);

            // 5) Backfill: every pre-existing row (including every existing User) belongs to the
            // one pre-existing tenant. AuditLogs is deliberately left null — historical entries
            // predate the tenant concept entirely and there's no reliable way to attribute them.
            migrationBuilder.Sql($@"
                UPDATE ""Users"" SET ""CompanyId"" = '{DefaultCompanyId}' WHERE ""CompanyId"" IS NULL;
                UPDATE ""Teams"" SET ""CompanyId"" = '{DefaultCompanyId}' WHERE ""CompanyId"" IS NULL;
                UPDATE ""Shifts"" SET ""CompanyId"" = '{DefaultCompanyId}' WHERE ""CompanyId"" IS NULL;
                UPDATE ""OfficeLocations"" SET ""CompanyId"" = '{DefaultCompanyId}' WHERE ""CompanyId"" IS NULL;
                UPDATE ""LeaveTypes"" SET ""CompanyId"" = '{DefaultCompanyId}' WHERE ""CompanyId"" IS NULL;
                UPDATE ""Employees"" SET ""CompanyId"" = '{DefaultCompanyId}' WHERE ""CompanyId"" IS NULL;
                UPDATE ""Designations"" SET ""CompanyId"" = '{DefaultCompanyId}' WHERE ""CompanyId"" IS NULL;
                UPDATE ""Departments"" SET ""CompanyId"" = '{DefaultCompanyId}' WHERE ""CompanyId"" IS NULL;
            ");

            // 6) Now that every row is populated, tighten the 8 core-entity tables to NOT NULL.
            // Users and AuditLogs stay nullable (see step 4).
            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "Teams",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "Shifts",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "OfficeLocations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "LeaveTypes",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "Employees",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "Designations",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "CompanyId",
                table: "Departments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // 7) Drop the old single-column unique indexes (global uniqueness no longer applies —
            // uniqueness is now per-company) and create the new composite ones.
            migrationBuilder.DropIndex(
                name: "IX_Teams_DepartmentId_Code",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_OfficeLocations_Code",
                table: "OfficeLocations");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_Code",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_Employees_Email",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Designations_Code",
                table: "Designations");

            migrationBuilder.DropIndex(
                name: "IX_Designations_Name",
                table: "Designations");

            migrationBuilder.DropIndex(
                name: "IX_Departments_Name",
                table: "Departments");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CompanyId",
                table: "Users",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CompanyId",
                table: "Teams",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CompanyId_DepartmentId_Code",
                table: "Teams",
                columns: new[] { "CompanyId", "DepartmentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DepartmentId",
                table: "Teams",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_CompanyId",
                table: "Shifts",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeLocations_CompanyId",
                table: "OfficeLocations",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeLocations_CompanyId_Code",
                table: "OfficeLocations",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_CompanyId",
                table: "LeaveTypes",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_CompanyId_Code",
                table: "LeaveTypes",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId",
                table: "Employees",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_Email",
                table: "Employees",
                columns: new[] { "CompanyId", "Email" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_CompanyId_EmployeeCode",
                table: "Employees",
                columns: new[] { "CompanyId", "EmployeeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Designations_CompanyId_Code",
                table: "Designations",
                columns: new[] { "CompanyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Designations_CompanyId_Name",
                table: "Designations",
                columns: new[] { "CompanyId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId",
                table: "Departments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_CompanyId_Name",
                table: "Departments",
                columns: new[] { "CompanyId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_CompanyId_CreatedAtUtc",
                table: "AuditLogs",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_RegisteredAtUtc",
                table: "Companies",
                column: "RegisteredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_Status",
                table: "Companies",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_AuditLogs_Companies_CompanyId",
                table: "AuditLogs",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Departments_Companies_CompanyId",
                table: "Departments",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Designations_Companies_CompanyId",
                table: "Designations",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_Companies_CompanyId",
                table: "Employees",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LeaveTypes_Companies_CompanyId",
                table: "LeaveTypes",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_OfficeLocations_Companies_CompanyId",
                table: "OfficeLocations",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Shifts_Companies_CompanyId",
                table: "Shifts",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_Companies_CompanyId",
                table: "Teams",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditLogs_Companies_CompanyId",
                table: "AuditLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Departments_Companies_CompanyId",
                table: "Departments");

            migrationBuilder.DropForeignKey(
                name: "FK_Designations_Companies_CompanyId",
                table: "Designations");

            migrationBuilder.DropForeignKey(
                name: "FK_Employees_Companies_CompanyId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_LeaveTypes_Companies_CompanyId",
                table: "LeaveTypes");

            migrationBuilder.DropForeignKey(
                name: "FK_OfficeLocations_Companies_CompanyId",
                table: "OfficeLocations");

            migrationBuilder.DropForeignKey(
                name: "FK_Shifts_Companies_CompanyId",
                table: "Shifts");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Companies_CompanyId",
                table: "Teams");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Companies_CompanyId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "PlatformSettings");

            migrationBuilder.DropIndex(
                name: "IX_Users_CompanyId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CompanyId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CompanyId_DepartmentId_Code",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_DepartmentId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_CompanyId",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_OfficeLocations_CompanyId",
                table: "OfficeLocations");

            migrationBuilder.DropIndex(
                name: "IX_OfficeLocations_CompanyId_Code",
                table: "OfficeLocations");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_CompanyId",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_LeaveTypes_CompanyId_Code",
                table: "LeaveTypes");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId_Email",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Employees_CompanyId_EmployeeCode",
                table: "Employees");

            migrationBuilder.DropIndex(
                name: "IX_Designations_CompanyId_Code",
                table: "Designations");

            migrationBuilder.DropIndex(
                name: "IX_Designations_CompanyId_Name",
                table: "Designations");

            migrationBuilder.DropIndex(
                name: "IX_Departments_CompanyId",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Departments_CompanyId_Name",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_CompanyId_CreatedAtUtc",
                table: "AuditLogs");

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: new Guid("55555555-5555-5555-5555-555555555555"));

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "OfficeLocations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Designations");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "AuditLogs");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_DepartmentId_Code",
                table: "Teams",
                columns: new[] { "DepartmentId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OfficeLocations_Code",
                table: "OfficeLocations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveTypes_Code",
                table: "LeaveTypes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Email",
                table: "Employees",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EmployeeCode",
                table: "Employees",
                column: "EmployeeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Designations_Code",
                table: "Designations",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Designations_Name",
                table: "Designations",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_Name",
                table: "Departments",
                column: "Name");
        }
    }
}
