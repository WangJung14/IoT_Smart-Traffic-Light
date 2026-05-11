using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrafficLight_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDetectionLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DetectionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    NsCars = table.Column<int>(type: "int", nullable: false),
                    NsMotorbikes = table.Column<int>(type: "int", nullable: false),
                    NsBuses = table.Column<int>(type: "int", nullable: false),
                    NsTrucks = table.Column<int>(type: "int", nullable: false),
                    EwCars = table.Column<int>(type: "int", nullable: false),
                    EwMotorbikes = table.Column<int>(type: "int", nullable: false),
                    EwBuses = table.Column<int>(type: "int", nullable: false),
                    EwTrucks = table.Column<int>(type: "int", nullable: false),
                    CalculatedCycleTime = table.Column<int>(type: "int", nullable: false),
                    CalculatedGreenNS = table.Column<int>(type: "int", nullable: false),
                    CalculatedGreenEW = table.Column<int>(type: "int", nullable: false),
                    TotalFlowRatio = table.Column<double>(type: "double", nullable: false),
                    Status = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Source = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectionLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectionLogs");
        }
    }
}
