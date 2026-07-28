using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollBonusAndOvertime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "OvertimeHours",
                table: "Payslips",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBonus",
                table: "Payslips",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOvertime",
                table: "Payslips",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OvertimeHours",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "TotalBonus",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "TotalOvertime",
                table: "Payslips");
        }
    }
}
