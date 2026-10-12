using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SMIS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCBCTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CBCReportCards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    AcademicTermId = table.Column<int>(type: "int", nullable: false),
                    AcademicYearId = table.Column<int>(type: "int", nullable: false),
                    ClassLevelId = table.Column<int>(type: "int", nullable: false),
                    ClassTeacherRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrincipalRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DaysPresent = table.Column<int>(type: "int", nullable: false),
                    DaysAbsent = table.Column<int>(type: "int", nullable: false),
                    TotalSchoolDays = table.Column<int>(type: "int", nullable: false),
                    Conduct = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Punctuality = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Neatness = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SportActivities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClubActivities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Talents = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextTermBegins = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NextClassPromotion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPromoted = table.Column<bool>(type: "bit", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CBCReportCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CBCReportCards_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CBCReportCards_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CBCReportCards_ClassLevels_ClassLevelId",
                        column: x => x.ClassLevelId,
                        principalTable: "ClassLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CBCReportCards_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CBCAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SubjectId = table.Column<int>(type: "int", nullable: false),
                    AcademicTermId = table.Column<int>(type: "int", nullable: false),
                    AcademicYearId = table.Column<int>(type: "int", nullable: false),
                    ClassLevelId = table.Column<int>(type: "int", nullable: false),
                    PerformanceLevel = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxScore = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TeacherRemarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StrandsAssessed = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssessmentType = table.Column<int>(type: "int", nullable: false),
                    CBCReportCardId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CBCAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CBCAssessments_AcademicTerms_AcademicTermId",
                        column: x => x.AcademicTermId,
                        principalTable: "AcademicTerms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CBCAssessments_AcademicYears_AcademicYearId",
                        column: x => x.AcademicYearId,
                        principalTable: "AcademicYears",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CBCAssessments_CBCReportCards_CBCReportCardId",
                        column: x => x.CBCReportCardId,
                        principalTable: "CBCReportCards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CBCAssessments_ClassLevels_ClassLevelId",
                        column: x => x.ClassLevelId,
                        principalTable: "ClassLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CBCAssessments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CBCAssessments_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CBCAssessments_AcademicTermId",
                table: "CBCAssessments",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCAssessments_AcademicYearId",
                table: "CBCAssessments",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCAssessments_CBCReportCardId",
                table: "CBCAssessments",
                column: "CBCReportCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCAssessments_ClassLevelId",
                table: "CBCAssessments",
                column: "ClassLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCAssessments_StudentId",
                table: "CBCAssessments",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCAssessments_SubjectId",
                table: "CBCAssessments",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCReportCards_AcademicTermId",
                table: "CBCReportCards",
                column: "AcademicTermId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCReportCards_AcademicYearId",
                table: "CBCReportCards",
                column: "AcademicYearId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCReportCards_ClassLevelId",
                table: "CBCReportCards",
                column: "ClassLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_CBCReportCards_StudentId",
                table: "CBCReportCards",
                column: "StudentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CBCAssessments");

            migrationBuilder.DropTable(
                name: "CBCReportCards");
        }
    }
}
