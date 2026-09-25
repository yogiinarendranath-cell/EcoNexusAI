using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoNexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWasteClassifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WasteClassifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CitizenProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Confidence = table.Column<double>(type: "float", nullable: false),
                    IsRecyclable = table.Column<bool>(type: "bit", nullable: false),
                    IsCompostable = table.Column<bool>(type: "bit", nullable: false),
                    DisposalInstruction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ImageReference = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ClassifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WasteClassifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WasteClassifications_CitizenProfiles_CitizenProfileId",
                        column: x => x.CitizenProfileId,
                        principalTable: "CitizenProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WasteClassifications_Category",
                table: "WasteClassifications",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_WasteClassifications_CitizenProfileId_ClassifiedAt",
                table: "WasteClassifications",
                columns: new[] { "CitizenProfileId", "ClassifiedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WasteClassifications_ProviderName_Confidence",
                table: "WasteClassifications",
                columns: new[] { "ProviderName", "Confidence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WasteClassifications");
        }
    }
}
