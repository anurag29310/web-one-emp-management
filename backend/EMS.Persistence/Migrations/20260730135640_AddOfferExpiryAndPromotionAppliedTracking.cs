using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EMS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOfferExpiryAndPromotionAppliedTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AppliedAtUtc",
                table: "Promotions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "Offers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Offers_ExpiresAtUtc",
                table: "Offers",
                column: "ExpiresAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Offers_ExpiresAtUtc",
                table: "Offers");

            migrationBuilder.DropColumn(
                name: "AppliedAtUtc",
                table: "Promotions");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "Offers");
        }
    }
}
