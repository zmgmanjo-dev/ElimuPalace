using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SMIS.Domain.Entities.Finance;
using SMIS.Domain.Entities.School;
using SMIS.Domain.Entities.Academic;
using SMIS.Domain.Enums;
namespace SMIS.Web.Services;

/// <summary>
/// Generates PDF documents for invoices and receipts
/// </summary>
public class PdfService
{
    public PdfService()
    {
        // Set QuestPDF license (Community is free)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates a PDF invoice for a student
    /// </summary>
    public byte[] GenerateInvoicePdf(StudentInvoice invoice, SchoolProfile? school)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    // ── HEADER ────────────────────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Row(logoRow =>
                        {
                            if (school?.LogoPath != null &&
                                File.Exists(school.LogoPath.TrimStart('/')))
                            {
                                logoRow.ConstantItem(60).Image(
                                    school.LogoPath.TrimStart('/'));
                            }
                            logoRow.RelativeItem().Column(textCol =>
                            {
                                textCol.Item().Text(school?.Name ?? "SMIS School")
                                    .FontSize(18).Bold().FontColor("#1a56db");
                            });
                        });
                            c.Item().Text(school?.Name ?? "SMIS School")
                                .FontSize(18).Bold().FontColor("#1a56db");
                            c.Item().Text(school?.Motto ?? "Excellence Through Knowledge")
                                .FontSize(10).Italic().FontColor("#6b7280");
                            c.Item().Text(school?.PhysicalAddress ?? "")
                                .FontSize(9).FontColor("#6b7280");
                            c.Item().Text($"Tel: {school?.PhoneNumber ?? ""}")
                                .FontSize(9).FontColor("#6b7280");
                            c.Item().Text($"Email: {school?.Email ?? ""}")
                                .FontSize(9).FontColor("#6b7280");
                        });

                        row.ConstantItem(150).Column(c =>
                        {
                            c.Item().Background("#1a56db").Padding(10).Column(inner =>
                            {
                                inner.Item().Text("FEE INVOICE")
                                    .FontSize(14).Bold().FontColor("#ffffff")
                                    .AlignCenter();
                                inner.Item().Text(invoice.InvoiceNumber)
                                    .FontSize(11).Bold().FontColor("#ffffff")
                                    .AlignCenter();
                                inner.Item().Text(invoice.Status.ToString().ToUpper())
                                    .FontSize(9).FontColor("#ffffff")
                                    .AlignCenter();
                            });
                        });
                    });

                    col.Item().PaddingVertical(10).LineHorizontal(1)
                        .LineColor("#e5e7eb");

                    // ── STUDENT & INVOICE INFO ────────────────
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("BILLED TO:").Bold().FontColor("#6b7280")
                                .FontSize(9);
                            c.Item().Text(invoice.Student.FullName)
                                .FontSize(13).Bold();
                            c.Item().Text($"Adm No: {invoice.Student.AdmissionNumber}")
                                .FontSize(10);
                            c.Item().Text($"Class: {invoice.Student.ClassLevel?.Name ?? "-"}")
                                .FontSize(10);
                            if (!string.IsNullOrEmpty(invoice.Student.FatherPhone))
                                c.Item().Text($"Tel: {invoice.Student.FatherPhone}")
                                    .FontSize(10);
                        });

                        row.ConstantItem(200).Column(c =>
                        {
                            c.Item().Text("INVOICE DETAILS:").Bold().FontColor("#6b7280")
                                .FontSize(9);
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Invoice No:").FontSize(10);
                                r.RelativeItem().Text(invoice.InvoiceNumber)
                                    .FontSize(10).Bold();
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Date:").FontSize(10);
                                r.RelativeItem()
                                    .Text(invoice.InvoiceDate.ToString("dd MMM yyyy"))
                                    .FontSize(10);
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Due Date:").FontSize(10);
                                r.RelativeItem()
                                    .Text(invoice.DueDate.ToString("dd MMM yyyy"))
                                    .FontSize(10).FontColor("#ef4444");
                            });
                            c.Item().Row(r =>
                            {
                                r.RelativeItem().Text("Term:").FontSize(10);
                                r.RelativeItem()
                                    .Text($"{invoice.AcademicYear?.Name} {invoice.AcademicTerm?.Name}")
                                    .FontSize(10);
                            });
                        });
                    });

                    col.Item().PaddingVertical(10);

                    // ── INVOICE ITEMS TABLE ───────────────────
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background("#1a56db").Padding(6)
                                .Text("#").FontColor("#ffffff").Bold().FontSize(9);
                            header.Cell().Background("#1a56db").Padding(6)
                                .Text("Description").FontColor("#ffffff").Bold().FontSize(9);
                            header.Cell().Background("#1a56db").Padding(6)
                                .Text("Amount").FontColor("#ffffff").Bold().FontSize(9)
                                .AlignRight();
                            header.Cell().Background("#1a56db").Padding(6)
                                .Text("Discount").FontColor("#ffffff").Bold().FontSize(9)
                                .AlignRight();
                            header.Cell().Background("#1a56db").Padding(6)
                                .Text("Net Amount").FontColor("#ffffff").Bold().FontSize(9)
                                .AlignRight();
                        });

                        // Items
                        var index = 1;
                        foreach (var item in invoice.Items)
                        {
                            var bg = index % 2 == 0 ? "#f9fafb" : "#ffffff";
                            table.Cell().Background(bg).Padding(6)
                                .Text(index.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(6)
                                .Text(item.Description).FontSize(9);
                            table.Cell().Background(bg).Padding(6)
                                .Text($"KES {item.Amount:N0}").FontSize(9).AlignRight();
                            table.Cell().Background(bg).Padding(6)
                                .Text(item.Discount > 0 ? $"KES {item.Discount:N0}" : "-")
                                .FontSize(9).AlignRight();
                            table.Cell().Background(bg).Padding(6)
                                .Text($"KES {item.NetAmount:N0}").FontSize(9).Bold()
                                .AlignRight();
                            index++;
                        }
                    });

                    col.Item().PaddingVertical(5);

                    // ── TOTALS ────────────────────────────────
                    col.Item().AlignRight().Width(250).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Background("#f3f4f6").Padding(6)
                            .Text("Total Invoiced:").Bold().FontSize(10);
                        table.Cell().Background("#f3f4f6").Padding(6)
                            .Text($"KES {invoice.TotalAmount:N0}").Bold()
                            .FontSize(10).AlignRight();

                        table.Cell().Padding(6)
                            .Text("Total Paid:").FontSize(10).FontColor("#057a55");
                        table.Cell().Padding(6)
                            .Text($"KES {invoice.TotalPaid:N0}").FontSize(10)
                            .FontColor("#057a55").AlignRight();

                        table.Cell().Background("#fef2f2").Padding(6)
                            .Text("Balance Due:").Bold().FontSize(11)
                            .FontColor("#dc2626");
                        table.Cell().Background("#fef2f2").Padding(6)
                            .Text($"KES {invoice.Balance:N0}").Bold()
                            .FontSize(11).FontColor("#dc2626").AlignRight();
                    });

                    col.Item().PaddingVertical(15);

                    // ── PAYMENT HISTORY ───────────────────────
                    if (invoice.Payments.Any())
                    {
                        col.Item().Text("PAYMENT HISTORY").Bold().FontSize(10)
                            .FontColor("#6b7280");
                        col.Item().PaddingVertical(4);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background("#057a55").Padding(5)
                                    .Text("Receipt No").FontColor("#ffffff")
                                    .Bold().FontSize(9);
                                header.Cell().Background("#057a55").Padding(5)
                                    .Text("Date").FontColor("#ffffff")
                                    .Bold().FontSize(9);
                                header.Cell().Background("#057a55").Padding(5)
                                    .Text("Method").FontColor("#ffffff")
                                    .Bold().FontSize(9);
                                header.Cell().Background("#057a55").Padding(5)
                                    .Text("Reference").FontColor("#ffffff")
                                    .Bold().FontSize(9);
                                header.Cell().Background("#057a55").Padding(5)
                                    .Text("Amount").FontColor("#ffffff")
                                    .Bold().FontSize(9).AlignRight();
                            });

                            foreach (var payment in invoice.Payments
                                .OrderByDescending(p => p.PaymentDate))
                            {
                                table.Cell().Padding(5)
                                    .Text(payment.ReceiptNumber).FontSize(9);
                                table.Cell().Padding(5)
                                    .Text(payment.PaymentDate.ToString("dd MMM yyyy"))
                                    .FontSize(9);
                                table.Cell().Padding(5)
                                    .Text(payment.PaymentMethod.ToString()).FontSize(9);
                                table.Cell().Padding(5)
                                    .Text(payment.ReferenceNumber ?? "-").FontSize(9);
                                table.Cell().Padding(5)
                                    .Text($"KES {payment.Amount:N0}").FontSize(9)
                                    .Bold().AlignRight();
                            }
                        });
                    }

                    col.Item().PaddingVertical(20);

                    // ── FOOTER ────────────────────────────────
                    col.Item().LineHorizontal(1).LineColor("#e5e7eb");
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Prepared by:").FontSize(9)
                                .FontColor("#6b7280");
                            c.Item().PaddingTop(20).Text("_________________")
                                .FontSize(9);
                            c.Item().Text("Bursar / Accounts").FontSize(9)
                                .FontColor("#6b7280");
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Official Stamp:").FontSize(9)
                                .FontColor("#6b7280");
                            c.Item().PaddingTop(20).Text("_________________")
                                .FontSize(9);
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Generated by SMIS").FontSize(8)
                                .FontColor("#9ca3af");
                            c.Item().Text(DateTime.Now.ToString("dd MMM yyyy HH:mm"))
                                .FontSize(8).FontColor("#9ca3af");
                        });
                    });
                });
            });
        }).GeneratePdf();
    }

    /// <summary>
    /// Generates a payment receipt PDF
    /// </summary>
    public byte[] GenerateReceiptPdf(FeePayment payment, SchoolProfile? school)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().AlignCenter().Column(c =>
                    {
                        c.Item().Text(school?.Name ?? "SMIS School")
                            .FontSize(16).Bold().FontColor("#1a56db")
                            .AlignCenter();
                        c.Item().Text(school?.Motto ?? "")
                            .FontSize(9).Italic().FontColor("#6b7280")
                            .AlignCenter();
                        c.Item().Text(school?.PhoneNumber ?? "")
                            .FontSize(9).FontColor("#6b7280").AlignCenter();
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(2)
                        .LineColor("#1a56db");

                    col.Item().Background("#1a56db").Padding(8)
                        .AlignCenter().Text("OFFICIAL RECEIPT")
                        .FontSize(14).Bold().FontColor("#ffffff");

                    col.Item().PaddingVertical(10);

                    // Receipt details
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(4)
                            .Text("Receipt No:").Bold().FontSize(10);
                        table.Cell().Padding(4)
                            .Text(payment.ReceiptNumber).FontSize(10).Bold()
                            .FontColor("#1a56db");

                        table.Cell().Padding(4)
                            .Text("Date:").Bold().FontSize(10);
                        table.Cell().Padding(4)
                            .Text(payment.PaymentDate.ToString("dd MMM yyyy"))
                            .FontSize(10);

                        table.Cell().Padding(4)
                            .Text("Student:").Bold().FontSize(10);
                        table.Cell().Padding(4)
                            .Text(payment.Student?.FullName ?? "-").FontSize(10);

                        table.Cell().Padding(4)
                            .Text("Adm No:").Bold().FontSize(10);
                        table.Cell().Padding(4)
                            .Text(payment.Student?.AdmissionNumber ?? "-")
                            .FontSize(10);

                        table.Cell().Padding(4)
                            .Text("Class:").Bold().FontSize(10);
                        table.Cell().Padding(4)
                            .Text(payment.Student?.ClassLevel?.Name ?? "-")
                            .FontSize(10);

                        table.Cell().Padding(4)
                            .Text("Invoice No:").Bold().FontSize(10);
                        table.Cell().Padding(4)
                            .Text(payment.StudentInvoice?.InvoiceNumber ?? "-")
                            .FontSize(10);

                        table.Cell().Padding(4)
                            .Text("Payment Method:").Bold().FontSize(10);
                        table.Cell().Padding(4)
                            .Text(payment.PaymentMethod.ToString()).FontSize(10);

                        if (!string.IsNullOrEmpty(payment.ReferenceNumber))
                        {
                            table.Cell().Padding(4)
                                .Text("Reference No:").Bold().FontSize(10);
                            table.Cell().Padding(4)
                                .Text(payment.ReferenceNumber).FontSize(10);
                        }
                    });

                    col.Item().PaddingVertical(10);

                    // Amount
                    col.Item().Background("#f0fdf4").Border(1)
                        .BorderColor("#057a55").Padding(12).Column(c =>
                    {
                        c.Item().Text("AMOUNT RECEIVED").FontSize(9)
                            .Bold().FontColor("#6b7280").AlignCenter();
                        c.Item().Text($"KES {payment.Amount:N0}")
                            .FontSize(22).Bold().FontColor("#057a55")
                            .AlignCenter();
                    });

                    col.Item().PaddingVertical(15);

                    // Signature
                    col.Item().LineHorizontal(1).LineColor("#e5e7eb");
                    col.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Received by:").FontSize(9)
                                .FontColor("#6b7280");
                            c.Item().PaddingTop(15)
                                .Text("_________________").FontSize(9);
                            c.Item().Text("Authorized Signature").FontSize(8)
                                .FontColor("#6b7280");
                        });
                        row.RelativeItem().AlignRight().Column(c =>
                        {
                            c.Item().Text("Official Stamp").FontSize(9)
                                .FontColor("#6b7280");
                            c.Item().PaddingTop(15)
                                .Text("_________________").FontSize(9);
                        });
                    });

                    col.Item().PaddingTop(10).Text("Thank you for your payment!")
                        .FontSize(9).Italic().FontColor("#6b7280").AlignCenter();

                    col.Item().Text("Generated by SMIS")
                        .FontSize(7).FontColor("#9ca3af").AlignCenter();
                });
            });
        }).GeneratePdf();
    }/// <summary>
    /// Generates a CBC Report Card PDF for a student
    /// </summary>
    public byte[] GenerateReportCardPdf(
        SMIS.Domain.Entities.Students.Student student,
        SMIS.Domain.Entities.School.AcademicTerm term,
        SMIS.Domain.Entities.School.SchoolProfile? school,
        List<CBCAssessment> assessments,
        CBCReportCard? reportCard,
        int daysPresent, int daysAbsent)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    // ── HEADER ────────────────────────────────
                    col.Item().Border(2).BorderColor("#1a56db")
                        .Padding(15).Column(header =>
                    {
                        header.Item().AlignCenter().Text(school?.Name ?? "SMIS School")
                            .FontSize(18).Bold().FontColor("#1a56db");
                        header.Item().AlignCenter()
                            .Text(school?.Motto ?? "Excellence Through Knowledge")
                            .FontSize(10).Italic().FontColor("#6b7280");
                        header.Item().AlignCenter()
                            .Text(school?.PhysicalAddress ?? "")
                            .FontSize(9).FontColor("#6b7280");
                        header.Item().PaddingTop(8).AlignCenter()
                            .Text("CBC LEARNER PROGRESS REPORT")
                            .FontSize(14).Bold().FontColor("#1a56db");
                        header.Item().AlignCenter()
                            .Text($"{term.AcademicYear?.Name} — {term.Name}")
                            .FontSize(11).Bold();
                    });

                    col.Item().PaddingVertical(8);

                    // ── STUDENT INFO ──────────────────────────
                    col.Item().Background("#f3f4f6").Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Name: {student.FullName}")
                                .FontSize(11).Bold();
                            c.Item().Text($"Adm No: {student.AdmissionNumber}")
                                .FontSize(10);
                            c.Item().Text($"Class: {student.ClassLevel?.Name ?? "-"}")
                                .FontSize(10);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Stream: {student.ClassStream?.Name ?? "-"}")
                                .FontSize(10);
                            c.Item().Text($"Days Present: {daysPresent}")
                                .FontSize(10).FontColor("#057a55");
                            c.Item().Text($"Days Absent: {daysAbsent}")
                                .FontSize(10).FontColor("#dc2626");
                        });
                    });

                    col.Item().PaddingVertical(8);

                    // ── ASSESSMENT TABLE ──────────────────────
                    col.Item().Text("LEARNING AREAS ASSESSMENT")
                        .FontSize(11).Bold().FontColor("#1a56db");
                    col.Item().PaddingVertical(4);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(25);
                            columns.RelativeColumn(3);
                            columns.ConstantColumn(50);
                            columns.ConstantColumn(60);
                            columns.RelativeColumn(2);
                        });

                        // Header
                        table.Header(header =>
                        {
                            header.Cell().Background("#1a56db").Padding(5)
                                .Text("#").FontColor("#ffffff").Bold().FontSize(9);
                            header.Cell().Background("#1a56db").Padding(5)
                                .Text("Learning Area").FontColor("#ffffff")
                                .Bold().FontSize(9);
                            header.Cell().Background("#1a56db").Padding(5)
                                .Text("Score").FontColor("#ffffff")
                                .Bold().FontSize(9).AlignCenter();
                            header.Cell().Background("#1a56db").Padding(5)
                                .Text("Performance").FontColor("#ffffff")
                                .Bold().FontSize(9).AlignCenter();
                            header.Cell().Background("#1a56db").Padding(5)
                                .Text("Remarks").FontColor("#ffffff")
                                .Bold().FontSize(9);
                        });

                        var index = 1;
                        foreach (var assessment in assessments)
                        {
                            var bg = index % 2 == 0 ? "#f9fafb" : "#ffffff";
                            var perfText = assessment.PerformanceLevel switch
                            {
                                PerformanceLevel.BelowExpectation => "BE",
                                PerformanceLevel.ApproachingExpectation => "AE",
                                PerformanceLevel.MeetExpectation => "ME",
                                PerformanceLevel.ExceedsExpectation => "EE",
                                _ => "-"
                            };
                            var perfColor = assessment.PerformanceLevel switch
                            {
                                PerformanceLevel.BelowExpectation => "#dc2626",
                                PerformanceLevel.ApproachingExpectation => "#d97706",
                                PerformanceLevel.MeetExpectation => "#057a55",
                                PerformanceLevel.ExceedsExpectation => "#1a56db",
                                _ => "#6b7280"
                            };

                            table.Cell().Background(bg).Padding(5)
                                .Text(index.ToString()).FontSize(9);
                            table.Cell().Background(bg).Padding(5)
                                .Text(assessment.Subject?.Name ?? "-").FontSize(9);
                            table.Cell().Background(bg).Padding(5).AlignCenter()
                                .Text(assessment.Score.HasValue
                                    ? $"{assessment.Score}/100" : "-")
                                .FontSize(9);
                            table.Cell().Background(bg).Padding(5).AlignCenter()
                                .Text(perfText).FontSize(10).Bold()
                                .FontColor(perfColor);
                            table.Cell().Background(bg).Padding(5)
                                .Text(assessment.TeacherRemarks ?? "-")
                                .FontSize(8).FontColor("#6b7280");
                            index++;
                        }
                    });

                    col.Item().PaddingVertical(8);

                    // ── PERFORMANCE KEY ───────────────────────
                    col.Item().Background("#f3f4f6").Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text("Performance Key:").Bold().FontSize(9);
                        row.RelativeItem().Text("BE = Below Expectation")
                            .FontSize(9).FontColor("#dc2626");
                        row.RelativeItem().Text("AE = Approaching Expectation")
                            .FontSize(9).FontColor("#d97706");
                        row.RelativeItem().Text("ME = Meets Expectation")
                            .FontSize(9).FontColor("#057a55");
                        row.RelativeItem().Text("EE = Exceeds Expectation")
                            .FontSize(9).FontColor("#1a56db");
                    });

                    col.Item().PaddingVertical(8);

                    // ── CHARACTER & ACTIVITIES ────────────────
                    if (reportCard != null)
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().ColumnSpan(4)
                                    .Background("#057a55").Padding(5)
                                    .Text("CHARACTER & CO-CURRICULAR ACTIVITIES")
                                    .FontColor("#ffffff").Bold().FontSize(9);
                            });

                            table.Cell().Background("#f9fafb").Padding(5)
                                .Text("Conduct:").Bold().FontSize(9);
                            table.Cell().Background("#f9fafb").Padding(5)
                                .Text(reportCard.Conduct ?? "-").FontSize(9);
                            table.Cell().Background("#f9fafb").Padding(5)
                                .Text("Punctuality:").Bold().FontSize(9);
                            table.Cell().Background("#f9fafb").Padding(5)
                                .Text(reportCard.Punctuality ?? "-").FontSize(9);

                            table.Cell().Padding(5)
                                .Text("Sports:").Bold().FontSize(9);
                            table.Cell().Padding(5)
                                .Text(reportCard.SportActivities ?? "-").FontSize(9);
                            table.Cell().Padding(5)
                                .Text("Clubs:").Bold().FontSize(9);
                            table.Cell().Padding(5)
                                .Text(reportCard.ClubActivities ?? "-").FontSize(9);
                        });

                        col.Item().PaddingVertical(8);

                        // ── REMARKS ───────────────────────────
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().ColumnSpan(2)
                                    .Background("#1a56db").Padding(5)
                                    .Text("REMARKS").FontColor("#ffffff")
                                    .Bold().FontSize(9);
                            });

                            table.Cell().Background("#f9fafb").Padding(8).Column(c =>
                            {
                                c.Item().Text("Class Teacher's Remarks:")
                                    .Bold().FontSize(9);
                                c.Item().PaddingTop(4)
                                    .Text(reportCard.ClassTeacherRemarks ?? "-")
                                    .FontSize(9);
                                c.Item().PaddingTop(20)
                                    .Text("Signature: _________________")
                                    .FontSize(8).FontColor("#6b7280");
                            });

                            table.Cell().Background("#ffffff").Padding(8).Column(c =>
                            {
                                c.Item().Text("Principal's Remarks:")
                                    .Bold().FontSize(9);
                                c.Item().PaddingTop(4)
                                    .Text(reportCard.PrincipalRemarks ?? "-")
                                    .FontSize(9);
                                c.Item().PaddingTop(20)
                                    .Text("Signature: _________________")
                                    .FontSize(8).FontColor("#6b7280");
                            });
                        });

                        col.Item().PaddingVertical(8);

                        // ── NEXT TERM ─────────────────────────
                        col.Item().Background("#f3f4f6").Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text(
                                $"Next Term Begins: " +
                                $"{reportCard.NextTermBegins?.ToString("dd MMM yyyy") ?? "TBA"}")
                                .FontSize(10).Bold();
                            row.RelativeItem().AlignRight().Text(
                                reportCard.IsPromoted
                                    ? "Status: PROMOTED ✓"
                                    : "Status: NOT PROMOTED")
                                .FontSize(10).Bold()
                                .FontColor(reportCard.IsPromoted ? "#057a55" : "#dc2626");
                        });
                    }

                    col.Item().PaddingVertical(10);

                    // ── FOOTER ────────────────────────────────
                    col.Item().LineHorizontal(1).LineColor("#e5e7eb");
                    col.Item().PaddingTop(5).AlignCenter()
                        .Text("Generated by SMIS — School Management Information System")
                        .FontSize(8).FontColor("#9ca3af");
                });
            });
        }).GeneratePdf();
    }
        /// <summary>
    /// Generates a payslip PDF for one staff member's payroll run line
    /// </summary>
    public byte[] GeneratePayslipPdf(
        SMIS.Domain.Entities.Accounting.PayrollRunLine line,
        int month, int year,
        SMIS.Domain.Entities.School.SchoolProfile? school)
    {
        var staff = line.StaffPayroll;
        var monthName = new DateTime(year, month, 1).ToString("MMMM yyyy");

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Item().AlignCenter().Column(c =>
                    {
                        c.Item().Text(school?.Name ?? "SMIS School")
                            .FontSize(16).Bold().FontColor("#1a56db").AlignCenter();
                        c.Item().Text(school?.PhysicalAddress ?? "")
                            .FontSize(9).FontColor("#6b7280").AlignCenter();
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(2)
                        .LineColor("#1a56db");

                    col.Item().Background("#1a56db").Padding(8)
                        .AlignCenter().Text($"PAYSLIP — {monthName}")
                        .FontSize(13).Bold().FontColor("#ffffff");

                    col.Item().PaddingVertical(10);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Padding(4).Text("Staff No:").Bold().FontSize(9);
                        table.Cell().Padding(4).Text(staff.StaffNumber).FontSize(9);

                        table.Cell().Padding(4).Text("Name:").Bold().FontSize(9);
                        table.Cell().Padding(4).Text(staff.FullName).FontSize(9);

                        table.Cell().Padding(4).Text("KRA PIN:").Bold().FontSize(9);
                        table.Cell().Padding(4).Text(staff.KRAPin ?? "-").FontSize(9);

                        table.Cell().Padding(4).Text("Bank:").Bold().FontSize(9);
                        table.Cell().Padding(4)
                            .Text($"{staff.BankName ?? "-"} — {staff.BankAccount ?? "-"}")
                            .FontSize(9);
                    });

                    col.Item().PaddingVertical(8);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#057a55").Padding(5)
                                .Text("EARNINGS").FontColor("#ffffff").Bold().FontSize(9);
                            header.Cell().Background("#057a55").Padding(5)
                                .Text("Amount").FontColor("#ffffff").Bold()
                                .FontSize(9).AlignRight();
                        });

                        table.Cell().Padding(4).Text("Basic Salary").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.BasicSalary:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("House Allowance").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.HouseAllowance:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("Transport Allowance").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.TransportAllowance:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("Medical Allowance").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.MedicalAllowance:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("Other Allowances").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.OtherAllowances:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Background("#f3f4f6").Padding(5)
                            .Text("GROSS PAY").Bold().FontSize(9);
                        table.Cell().Background("#f3f4f6").Padding(5)
                            .Text($"{line.GrossSalary:N2}").Bold()
                            .FontSize(9).AlignRight();
                    });

                    col.Item().PaddingVertical(6);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#dc2626").Padding(5)
                                .Text("DEDUCTIONS").FontColor("#ffffff").Bold().FontSize(9);
                            header.Cell().Background("#dc2626").Padding(5)
                                .Text("Amount").FontColor("#ffffff").Bold()
                                .FontSize(9).AlignRight();
                        });

                        table.Cell().Padding(4).Text("PAYE").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.PAYE:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("NSSF").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.NSSFEmployee:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("SHA").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.SHAEmployee:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("SACCO").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.SACCODeduction:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("Loan").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.LoanDeduction:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Padding(4).Text("Other").FontSize(9);
                        table.Cell().Padding(4).Text($"{line.OtherDeductions:N2}")
                            .FontSize(9).AlignRight();

                        table.Cell().Background("#f3f4f6").Padding(5)
                            .Text("TOTAL DEDUCTIONS").Bold().FontSize(9);
                        table.Cell().Background("#f3f4f6").Padding(5)
                            .Text($"{line.TotalDeductions:N2}").Bold()
                            .FontSize(9).AlignRight();
                    });

                    col.Item().PaddingVertical(10);

                    col.Item().Background("#f0fdf4").Border(1)
                        .BorderColor("#057a55").Padding(12).Column(c =>
                    {
                        c.Item().Text("NET PAY").FontSize(9).Bold()
                            .FontColor("#6b7280").AlignCenter();
                        c.Item().Text($"KES {line.NetPay:N2}")
                            .FontSize(20).Bold().FontColor("#057a55").AlignCenter();
                    });

                    col.Item().PaddingVertical(15);
                    col.Item().LineHorizontal(1).LineColor("#e5e7eb");
                    col.Item().PaddingTop(8).AlignCenter()
                        .Text("This is a computer-generated payslip. Generated by SMIS")
                        .FontSize(7).FontColor("#9ca3af");
                });
            });
        }).GeneratePdf();
    }
    /// <summary>
/// Generates a generic tabular report PDF (used for Reports Centre exports:
/// Fee Collection, Outstanding Fees, Daily Collection, Class List, etc.)
/// </summary>
public byte[] GenerateTableReportPdf(
    string title,
    string subtitle,
    List<string> columnHeaders,
    List<List<string>> rows,
    SchoolProfile? school,
    List<(string Label, string Value)>? summaryStats = null,
    string? footerNote = null)
{
    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(school?.Name ?? "SMIS School")
                            .FontSize(16).Bold().FontColor("#1a56db");
                        c.Item().Text(school?.PhysicalAddress ?? "")
                            .FontSize(8).FontColor("#6b7280");
                    });
                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text(title).FontSize(14).Bold();
                        c.Item().Text(subtitle).FontSize(9).FontColor("#6b7280");
                    });
                });
                col.Item().PaddingVertical(6).LineHorizontal(1).LineColor("#e5e7eb");
            });

            page.Content().Column(col =>
            {
                // ── Summary stat strip ──
                if (summaryStats != null && summaryStats.Any())
                {
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        foreach (var stat in summaryStats)
                        {
                            row.RelativeItem().Background("#f3f4f6").Padding(8).Column(c =>
                            {
                                c.Item().Text(stat.Value).FontSize(12).Bold()
                                    .FontColor("#1a56db");
                                c.Item().Text(stat.Label).FontSize(8)
                                    .FontColor("#6b7280");
                            });
                        }
                    });
                }

                // ── Table ──
                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        foreach (var _ in columnHeaders)
                            columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        foreach (var h in columnHeaders)
                        {
                            header.Cell().Background("#1a56db").Padding(5)
                                .Text(h).FontColor("#ffffff").Bold().FontSize(8);
                        }
                    });

                    var index = 0;
                    foreach (var row in rows)
                    {
                        var bg = index % 2 == 0 ? "#ffffff" : "#f9fafb";
                        foreach (var cell in row)
                        {
                            table.Cell().Background(bg).Padding(5)
                                .Text(cell).FontSize(8);
                        }
                        index++;
                    }
                });

                if (!rows.Any())
                {
                    col.Item().PaddingTop(20).AlignCenter()
                        .Text("No records found for the selected filters.")
                        .FontSize(10).FontColor("#6b7280");
                }
            });

            page.Footer().Column(col =>
            {
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor("#e5e7eb");
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text(footerNote ?? "Generated by SMIS")
                        .FontSize(7).FontColor("#9ca3af");
                    row.RelativeItem().AlignRight()
                        .Text(DateTime.Now.ToString("dd MMM yyyy HH:mm"))
                        .FontSize(7).FontColor("#9ca3af");
                });
            });
        });
    }).GeneratePdf();
}
/// <summary>
/// Generates a PDF report with multiple titled table sections on one document
/// (used for reports like Student Statement — invoices + payments,
/// or Library Report — categories + recent issues).
/// </summary>
public byte[] GenerateMultiTableReportPdf(
    string title,
    string subtitle,
    List<(string SectionTitle, List<string> Headers, List<List<string>> Rows)> sections,
    SchoolProfile? school,
    List<(string Label, string Value)>? summaryStats = null,
    string? footerNote = null)
{
    return Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(9));

            page.Header().Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text(school?.Name ?? "SMIS School")
                            .FontSize(16).Bold().FontColor("#1a56db");
                        c.Item().Text(school?.PhysicalAddress ?? "")
                            .FontSize(8).FontColor("#6b7280");
                    });
                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text(title).FontSize(14).Bold();
                        c.Item().Text(subtitle).FontSize(9).FontColor("#6b7280");
                    });
                });
                col.Item().PaddingVertical(6).LineHorizontal(1).LineColor("#e5e7eb");
            });

            page.Content().Column(col =>
            {
                if (summaryStats != null && summaryStats.Any())
                {
                    col.Item().PaddingBottom(10).Row(row =>
                    {
                        foreach (var stat in summaryStats)
                        {
                            row.RelativeItem().Background("#f3f4f6").Padding(8).Column(c =>
                            {
                                c.Item().Text(stat.Value).FontSize(12).Bold()
                                    .FontColor("#1a56db");
                                c.Item().Text(stat.Label).FontSize(8)
                                    .FontColor("#6b7280");
                            });
                        }
                    });
                }

                foreach (var section in sections)
                {
                    col.Item().PaddingTop(10).Text(section.SectionTitle)
                        .FontSize(11).Bold().FontColor("#1a56db");
                    col.Item().PaddingBottom(4);

                    if (!section.Rows.Any())
                    {
                        col.Item().PaddingBottom(10).Text("No records.")
                            .FontSize(9).FontColor("#6b7280");
                        continue;
                    }

                    col.Item().PaddingBottom(10).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            foreach (var _ in section.Headers)
                                columns.RelativeColumn();
                        });

                        table.Header(header =>
                        {
                            foreach (var h in section.Headers)
                            {
                                header.Cell().Background("#1a56db").Padding(5)
                                    .Text(h).FontColor("#ffffff").Bold().FontSize(8);
                            }
                        });

                        var index = 0;
                        foreach (var row in section.Rows)
                        {
                            var bg = index % 2 == 0 ? "#ffffff" : "#f9fafb";
                            foreach (var cell in row)
                            {
                                table.Cell().Background(bg).Padding(5)
                                    .Text(cell).FontSize(8);
                            }
                            index++;
                        }
                    });
                }
            });

            page.Footer().Column(col =>
            {
                col.Item().PaddingTop(5).LineHorizontal(1).LineColor("#e5e7eb");
                col.Item().PaddingTop(4).Row(row =>
                {
                    row.RelativeItem().Text(footerNote ?? "Generated by SMIS")
                        .FontSize(7).FontColor("#9ca3af");
                    row.RelativeItem().AlignRight()
                        .Text(DateTime.Now.ToString("dd MMM yyyy HH:mm"))
                        .FontSize(7).FontColor("#9ca3af");
                });
            });
        });
    }).GeneratePdf();
}
}