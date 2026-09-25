using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoNexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenEconomy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CitizenProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    HomeAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    HomeLatitude = table.Column<double>(type: "float", nullable: true),
                    HomeLongitude = table.Column<double>(type: "float", nullable: true),
                    CurrentStreakDays = table.Column<int>(type: "int", nullable: false),
                    LastVisitDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Rewards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CostInPoints = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GreenPointTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CitizenProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignedDelta = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RelatedEntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GreenPointTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GreenPointTransactions_CitizenProfiles_CitizenProfileId",
                        column: x => x.CitizenProfileId,
                        principalTable: "CitizenProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "UX_CitizenProfiles_UserId",
                table: "CitizenProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GreenPointTransactions_CitizenProfileId_OccurredAt",
                table: "GreenPointTransactions",
                columns: new[] { "CitizenProfileId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GreenPointTransactions_CitizenProfileId_Reason_OccurredAt",
                table: "GreenPointTransactions",
                columns: new[] { "CitizenProfileId", "Reason", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_GreenPointTransactions_Reason",
                table: "GreenPointTransactions",
                column: "Reason");

            migrationBuilder.CreateIndex(
                name: "IX_GreenPointTransactions_Source",
                table: "GreenPointTransactions",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_Rewards_IsActive",
                table: "Rewards",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GreenPointTransactions");

            migrationBuilder.DropTable(
                name: "Rewards");

            migrationBuilder.DropTable(
                name: "CitizenProfiles");
        }
    }
}
