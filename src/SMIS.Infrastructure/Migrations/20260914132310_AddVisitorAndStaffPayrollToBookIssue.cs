using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMIS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVisitorAndStaffPayrollToBookIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StaffPayrollId",
                table: "BookIssues",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitorIdNumber",
                table: "BookIssues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitorName",
                table: "BookIssues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisitorPosition",
                table: "BookIssues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookIssues_StaffPayrollId",
                table: "BookIssues",
                column: "StaffPayrollId");

            migrationBuilder.AddForeignKey(
                name: "FK_BookIssues_StaffPayrolls_StaffPayrollId",
                table: "BookIssues",
                column: "StaffPayrollId",
                principalTable: "StaffPayrolls",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BookIssues_StaffPayrolls_StaffPayrollId",
                table: "BookIssues");

            migrationBuilder.DropIndex(
                name: "IX_BookIssues_StaffPayrollId",
                table: "BookIssues");

            migrationBuilder.DropColumn(
                name: "StaffPayrollId",
                table: "BookIssues");

            migrationBuilder.DropColumn(
                name: "VisitorIdNumber",
                table: "BookIssues");

            migrationBuilder.DropColumn(
                name: "VisitorName",
                table: "BookIssues");

            migrationBuilder.DropColumn(
                name: "VisitorPosition",
                table: "BookIssues");
        }
    }
}
