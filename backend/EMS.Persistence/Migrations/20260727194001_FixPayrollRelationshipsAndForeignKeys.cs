using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPayrollRelationshipsAndForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Allowances_SalaryStructures_SalaryStructureId1",
                table: "Allowances");

            migrationBuilder.DropForeignKey(
                name: "FK_Deductions_SalaryStructures_SalaryStructureId1",
                table: "Deductions");

            migrationBuilder.DropForeignKey(
                name: "FK_Payslips_PayrollRuns_PayrollRunId1",
                table: "Payslips");

            migrationBuilder.DropIndex(
                name: "IX_Payslips_PayrollRunId1",
                table: "Payslips");

            migrationBuilder.DropIndex(
                name: "IX_Deductions_SalaryStructureId1",
                table: "Deductions");

            migrationBuilder.DropIndex(
                name: "IX_Allowances_SalaryStructureId1",
                table: "Allowances");

            migrationBuilder.DropColumn(
                name: "PayrollRunId1",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "SalaryStructureId1",
                table: "Deductions");

            migrationBuilder.DropColumn(
                name: "SalaryStructureId1",
                table: "Allowances");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryStructures_EmployeeId",
                table: "SalaryStructures",
                column: "EmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payslips_Employees_EmployeeId",
                table: "Payslips",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryStructures_Employees_EmployeeId",
                table: "SalaryStructures",
                column: "EmployeeId",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payslips_Employees_EmployeeId",
                table: "Payslips");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryStructures_Employees_EmployeeId",
                table: "SalaryStructures");

            migrationBuilder.DropIndex(
                name: "IX_SalaryStructures_EmployeeId",
                table: "SalaryStructures");

            migrationBuilder.AddColumn<Guid>(
                name: "PayrollRunId1",
                table: "Payslips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalaryStructureId1",
                table: "Deductions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalaryStructureId1",
                table: "Allowances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_PayrollRunId1",
                table: "Payslips",
                column: "PayrollRunId1");

            migrationBuilder.CreateIndex(
                name: "IX_Deductions_SalaryStructureId1",
                table: "Deductions",
                column: "SalaryStructureId1");

            migrationBuilder.CreateIndex(
                name: "IX_Allowances_SalaryStructureId1",
                table: "Allowances",
                column: "SalaryStructureId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Allowances_SalaryStructures_SalaryStructureId1",
                table: "Allowances",
                column: "SalaryStructureId1",
                principalTable: "SalaryStructures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Deductions_SalaryStructures_SalaryStructureId1",
                table: "Deductions",
                column: "SalaryStructureId1",
                principalTable: "SalaryStructures",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payslips_PayrollRuns_PayrollRunId1",
                table: "Payslips",
                column: "PayrollRunId1",
                principalTable: "PayrollRuns",
                principalColumn: "Id");
        }
    }
}
