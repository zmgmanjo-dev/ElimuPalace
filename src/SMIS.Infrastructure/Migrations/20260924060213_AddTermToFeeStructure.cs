using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMIS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTermToFeeStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<int>(
        name: "AcademicTermId",
        table: "FeeStructures",
        type: "int",
        nullable: false,
        defaultValue: 0);

    // Backfill existing rows with the current term's ID before enforcing the FK
    migrationBuilder.Sql(@"
    UPDATE FeeStructures
    SET AcademicTermId = (SELECT TOP 1 Id FROM AcademicTerms WHERE IsCurrent = 1)
    WHERE AcademicTermId = 0;");

    migrationBuilder.CreateIndex(
        name: "IX_FeeStructures_AcademicTermId",
        table: "FeeStructures",
        column: "AcademicTermId");

    migrationBuilder.AddForeignKey(
        name: "FK_FeeStructures_AcademicTerms_AcademicTermId",
        table: "FeeStructures",
        column: "AcademicTermId",
        principalTable: "AcademicTerms",
        principalColumn: "Id",
        onDelete: ReferentialAction.Restrict);
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeeStructures_AcademicTerms_AcademicTermId",
                table: "FeeStructures");

            migrationBuilder.DropIndex(
                name: "IX_FeeStructures_AcademicTermId",
                table: "FeeStructures");

            migrationBuilder.DropColumn(
                name: "AcademicTermId",
                table: "FeeStructures");
        }
    }
}
