using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MedVaultAPI.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAgeWithDateOfBirth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Age",
                table: "HealthProfiles");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateOfBirth",
                table: "HealthProfiles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                table: "HealthProfiles");

            migrationBuilder.AddColumn<int>(
                name: "Age",
                table: "HealthProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
