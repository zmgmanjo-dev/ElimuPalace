using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMIS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RedesignCashbook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashbookEntries_Accounts_AccountId",
                table: "CashbookEntries");

            migrationBuilder.RenameColumn(
                name: "PaidFrom",
                table: "SupplierPayments",
                newName: "PaidFromAccountId");

            migrationBuilder.RenameColumn(
                name: "PaidFrom",
                table: "Expenses",
                newName: "PaidFromAccountId");

            migrationBuilder.RenameColumn(
                name: "CashAccount",
                table: "CashbookEntries",
                newName: "ContraAccountId");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "CashbookEntries",
                newName: "CashAccountId");

            migrationBuilder.RenameIndex(
                name: "IX_CashbookEntries_AccountId",
                table: "CashbookEntries",
                newName: "IX_CashbookEntries_CashAccountId");

            migrationBuilder.AddColumn<string>(
                name: "AccountNumber",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "Accounts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierPayments_PaidFromAccountId",
                table: "SupplierPayments",
                column: "PaidFromAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_PaidFromAccountId",
                table: "Expenses",
                column: "PaidFromAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CashbookEntries_ContraAccountId",
                table: "CashbookEntries",
                column: "ContraAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashbookEntries_Accounts_CashAccountId",
                table: "CashbookEntries",
                column: "CashAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CashbookEntries_Accounts_ContraAccountId",
                table: "CashbookEntries",
                column: "ContraAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_Accounts_PaidFromAccountId",
                table: "Expenses",
                column: "PaidFromAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierPayments_Accounts_PaidFromAccountId",
                table: "SupplierPayments",
                column: "PaidFromAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashbookEntries_Accounts_CashAccountId",
                table: "CashbookEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_CashbookEntries_Accounts_ContraAccountId",
                table: "CashbookEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_Accounts_PaidFromAccountId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierPayments_Accounts_PaidFromAccountId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_SupplierPayments_PaidFromAccountId",
                table: "SupplierPayments");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_PaidFromAccountId",
                table: "Expenses");

            migrationBuilder.DropIndex(
                name: "IX_CashbookEntries_ContraAccountId",
                table: "CashbookEntries");

            migrationBuilder.DropColumn(
                name: "AccountNumber",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "Accounts");

            migrationBuilder.RenameColumn(
                name: "PaidFromAccountId",
                table: "SupplierPayments",
                newName: "PaidFrom");

            migrationBuilder.RenameColumn(
                name: "PaidFromAccountId",
                table: "Expenses",
                newName: "PaidFrom");

            migrationBuilder.RenameColumn(
                name: "ContraAccountId",
                table: "CashbookEntries",
                newName: "CashAccount");

            migrationBuilder.RenameColumn(
                name: "CashAccountId",
                table: "CashbookEntries",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_CashbookEntries_CashAccountId",
                table: "CashbookEntries",
                newName: "IX_CashbookEntries_AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashbookEntries_Accounts_AccountId",
                table: "CashbookEntries",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
