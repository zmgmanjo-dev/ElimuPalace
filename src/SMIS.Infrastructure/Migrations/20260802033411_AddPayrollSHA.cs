using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMIS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollSHA : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "NHIFNumber",
                table: "StaffPayrolls",
                newName: "shaNumber");

            migrationBuilder.RenameColumn(
                name: "TotalNHIF",
                table: "PayrollRuns",
                newName: "TotalSHA");

            migrationBuilder.RenameColumn(
                name: "NHIFEmployer",
                table: "PayrollRunLines",
                newName: "SHAEmployer");

            migrationBuilder.RenameColumn(
                name: "NHIFEmployee",
                table: "PayrollRunLines",
                newName: "SHAEmployee");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "shaNumber",
                table: "StaffPayrolls",
                newName: "NHIFNumber");

            migrationBuilder.RenameColumn(
                name: "TotalSHA",
                table: "PayrollRuns",
                newName: "TotalNHIF");

            migrationBuilder.RenameColumn(
                name: "SHAEmployer",
                table: "PayrollRunLines",
                newName: "NHIFEmployer");

            migrationBuilder.RenameColumn(
                name: "SHAEmployee",
                table: "PayrollRunLines",
                newName: "NHIFEmployee");
        }
    }
}
