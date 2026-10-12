using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMIS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFeePaymentJournalLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "JournalEntryId",
                table: "FeePayments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeePayments_JournalEntryId",
                table: "FeePayments",
                column: "JournalEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_FeePayments_JournalEntries_JournalEntryId",
                table: "FeePayments",
                column: "JournalEntryId",
                principalTable: "JournalEntries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeePayments_JournalEntries_JournalEntryId",
                table: "FeePayments");

            migrationBuilder.DropIndex(
                name: "IX_FeePayments_JournalEntryId",
                table: "FeePayments");

            migrationBuilder.DropColumn(
                name: "JournalEntryId",
                table: "FeePayments");
        }
    }
}
