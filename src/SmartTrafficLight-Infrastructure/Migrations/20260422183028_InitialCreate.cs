using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartTrafficLight_Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Intersections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Location = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NumberOfLanes = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intersections", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PredictionResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IntersectionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SuggestedGreen = table.Column<int>(type: "int", nullable: false),
                    SuggestedYellow = table.Column<int>(type: "int", nullable: false),
                    SuggestedRed = table.Column<int>(type: "int", nullable: false),
                    ConfidenceScore = table.Column<double>(type: "double", nullable: false),
                    PredictAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionResults_Intersections_IntersectionId",
                        column: x => x.IntersectionId,
                        principalTable: "Intersections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrafficDatas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IntersectionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Direction = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VehicleCount = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficDatas_Intersections_IntersectionId",
                        column: x => x.IntersectionId,
                        principalTable: "Intersections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TrafficLights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IntersectionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Direction = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CurrentState = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GreenDuration = table.Column<int>(type: "int", nullable: false),
                    YellowDuration = table.Column<int>(type: "int", nullable: false),
                    RedDuration = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrafficLights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrafficLights_Intersections_IntersectionId",
                        column: x => x.IntersectionId,
                        principalTable: "Intersections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Intersections",
                columns: new[] { "Id", "Location", "Name", "NumberOfLanes" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "Ho Chi Minh City", "Nga Tu Hang Xanh", 4 });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionResults_IntersectionId",
                table: "PredictionResults",
                column: "IntersectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficDatas_IntersectionId",
                table: "TrafficDatas",
                column: "IntersectionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficDatas_Timestamp",
                table: "TrafficDatas",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TrafficLights_IntersectionId",
                table: "TrafficLights",
                column: "IntersectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionResults");

            migrationBuilder.DropTable(
                name: "TrafficDatas");

            migrationBuilder.DropTable(
                name: "TrafficLights");

            migrationBuilder.DropTable(
                name: "Intersections");
        }
    }
}
