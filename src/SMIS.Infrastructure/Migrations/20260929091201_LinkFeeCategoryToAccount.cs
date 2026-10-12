using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMIS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkFeeCategoryToAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "IncomeAccountId",
                table: "FeeCategories",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeeCategories_IncomeAccountId",
                table: "FeeCategories",
                column: "IncomeAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_FeeCategories_Accounts_IncomeAccountId",
                table: "FeeCategories",
                column: "IncomeAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeeCategories_Accounts_IncomeAccountId",
                table: "FeeCategories");

            migrationBuilder.DropIndex(
                name: "IX_FeeCategories_IncomeAccountId",
                table: "FeeCategories");

            migrationBuilder.DropColumn(
                name: "IncomeAccountId",
                table: "FeeCategories");
        }
    }
}
