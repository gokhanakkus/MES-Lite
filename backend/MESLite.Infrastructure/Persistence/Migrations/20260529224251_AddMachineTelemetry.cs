using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMachineTelemetry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CycleTimeSeconds",
                table: "Machines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Efficiency",
                table: "Machines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "HealthScore",
                table: "Machines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Load",
                table: "Machines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Rpm",
                table: "Machines",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Speed",
                table: "Machines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Temperature",
                table: "Machines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Vibration",
                table: "Machines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "WearLevel",
                table: "Machines",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CycleTimeSeconds",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Efficiency",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "HealthScore",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Load",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Rpm",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Speed",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Temperature",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "Vibration",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "WearLevel",
                table: "Machines");
        }
    }
}
