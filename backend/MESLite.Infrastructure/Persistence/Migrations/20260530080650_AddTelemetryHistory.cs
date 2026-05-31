using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelemetrySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HealthScore = table.Column<double>(type: "float", nullable: false),
                    WearLevel = table.Column<double>(type: "float", nullable: false),
                    Rpm = table.Column<int>(type: "int", nullable: false),
                    Speed = table.Column<double>(type: "float", nullable: false),
                    Load = table.Column<double>(type: "float", nullable: false),
                    Vibration = table.Column<double>(type: "float", nullable: false),
                    Temperature = table.Column<double>(type: "float", nullable: false),
                    Efficiency = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetrySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TelemetrySnapshots_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySnapshots_CreatedAt",
                table: "TelemetrySnapshots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySnapshots_MachineId_CreatedAt",
                table: "TelemetrySnapshots",
                columns: new[] { "MachineId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelemetrySnapshots");
        }
    }
}
