using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcoNexus.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenReportType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReportType",
                table: "CitizenReports",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CitizenReports_ReportType",
                table: "CitizenReports",
                column: "ReportType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CitizenReports_ReportType",
                table: "CitizenReports");

            migrationBuilder.DropColumn(
                name: "ReportType",
                table: "CitizenReports");
        }
    }
}
