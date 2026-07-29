using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollAuditAndSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "SalaryStructures",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "SalaryStructures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAtUtc",
                table: "SalaryStructures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                table: "SalaryStructures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "SalaryStructures",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "SalaryStructures",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "SalaryStructures",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "SalaryStructures",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "PayrollRuns",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "PayrollRuns",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Deductions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Allowances",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructures_IsDeleted",
                table: "SalaryStructures",
                column: "IsDeleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SalaryStructures_IsDeleted",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "DeletedAtUtc",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "SalaryStructures");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "PayrollRuns");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Deductions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Allowances");
        }
    }
}
