using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoNexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRecyclingFacility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecyclingFacilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    DailyCapacityKilograms = table.Column<double>(type: "float", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecyclingFacilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacilityIntakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacilityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Material = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    WeightKilograms = table.Column<double>(type: "float", nullable: false),
                    Stage = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StageUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacilityIntakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FacilityIntakes_RecyclingFacilities_FacilityId",
                        column: x => x.FacilityId,
                        principalTable: "RecyclingFacilities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityIntakes_FacilityId_RecordedAt",
                table: "FacilityIntakes",
                columns: new[] { "FacilityId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FacilityIntakes_Stage",
                table: "FacilityIntakes",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_RecyclingFacilities_Status",
                table: "RecyclingFacilities",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FacilityIntakes");

            migrationBuilder.DropTable(
                name: "RecyclingFacilities");
        }
    }
}
