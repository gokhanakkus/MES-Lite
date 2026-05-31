using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MESLite.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAlarms : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alarms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MachineId = table.Column<int>(type: "int", nullable: false),
                    Metric = table.Column<int>(type: "int", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Detector = table.Column<int>(type: "int", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Value = table.Column<double>(type: "float", nullable: false),
                    Limit = table.Column<double>(type: "float", nullable: false),
                    RaisedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alarms_Machines_MachineId",
                        column: x => x.MachineId,
                        principalTable: "Machines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_MachineId_ResolvedAt",
                table: "Alarms",
                columns: new[] { "MachineId", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Alarms_RaisedAt",
                table: "Alarms",
                column: "RaisedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alarms");
        }
    }
}
