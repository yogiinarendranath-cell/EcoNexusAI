using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoNexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAssistantInteractions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssistantInteractions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Question = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ToolName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToolParametersJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ToolResultJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LatencyMs = table.Column<long>(type: "bigint", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantInteractions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantInteractions_UserId",
                table: "AssistantInteractions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AssistantInteractions_UserId_OccurredAt",
                table: "AssistantInteractions",
                columns: new[] { "UserId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantInteractions");
        }
    }
}
