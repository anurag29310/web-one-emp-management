using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfficeGeofencingAndMileageReimbursement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "Reimbursements",
                type: "numeric(10,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MileageRatePerKm",
                table: "Reimbursements",
                type: "numeric(10,4)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GeofenceRadiusMeters",
                table: "OfficeLocations",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "OfficeLocations",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "OfficeLocations",
                type: "numeric(9,6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "MileageRatePerKm",
                table: "Reimbursements");

            migrationBuilder.DropColumn(
                name: "GeofenceRadiusMeters",
                table: "OfficeLocations");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "OfficeLocations");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "OfficeLocations");
        }
    }
}
