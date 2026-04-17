using Boilerate.Application.Common.Exporters;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Boilerate.Infrastructure.Exporters;

/// <summary>
/// PDF service implementation using QuestPDF
/// </summary>
public class QuestPdfService : IPdfService
{
    private readonly ILogger<QuestPdfService> _logger;

    public QuestPdfService(ILogger<QuestPdfService> logger)
    {
        _logger = logger;
        
        // Configure QuestPDF license (Community license is free for individuals and small companies)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc/>
    public Task<byte[]> CreateInvoicePdfAsync(
        InvoiceData data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PdfOptions { Title = $"Invoice {data.InvoiceNumber}" };

        try
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    if (options.IncludeHeader)
                    {
                        page.Header().Element(c => ComposeInvoiceHeader(c, data));
                    }

                    // Content
                    page.Content().Element(c => ComposeInvoiceContent(c, data));

                    // Footer
                    if (options.IncludeFooter)
                    {
                        page.Footer().Element(c => ComposeFooter(c, options));
                    }
                });
            });

            var pdfBytes = document.GeneratePdf();

            _logger.LogInformation("Generated invoice PDF: {InvoiceNumber}", data.InvoiceNumber);
            return Task.FromResult(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating invoice PDF: {InvoiceNumber}", data.InvoiceNumber);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> CreateReportPdfAsync(
        ReportData data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PdfOptions { Title = data.Title };

        try
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // Header
                    if (options.IncludeHeader)
                    {
                        page.Header().Element(c => ComposeReportHeader(c, data, options));
                    }

                    // Content
                    page.Content().Element(c => ComposeReportContent(c, data));

                    // Footer
                    if (options.IncludeFooter)
                    {
                        page.Footer().Element(c => ComposeFooter(c, options));
                    }
                });
            });

            var pdfBytes = document.GeneratePdf();

            _logger.LogInformation("Generated report PDF: {Title}", data.Title);
            return Task.FromResult(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating report PDF: {Title}", data.Title);
            throw;
        }
    }

    /// <inheritdoc/>
    public Task<byte[]> GenerateFromHtmlAsync(
        string html,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("HTML to PDF conversion requires additional library like DinkToPdf or PuppeteerSharp");
    }

    /// <inheritdoc/>
    public Task<byte[]> MergePdfsAsync(
        List<byte[]> pdfFiles,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("PDF merging requires additional library like iTextSharp or PdfSharp");
    }

    /// <inheritdoc/>
    public Task<byte[]> AddWatermarkAsync(
        byte[] pdfFile,
        string watermarkText,
        CancellationToken cancellationToken = default)
    {
        // QuestPDF is better at generating from scratch, but we can layer content.
        // For production watermark on existing PDFs, iText is recommended.
        throw new NotImplementedException("Adding watermark to existing PDF is not supported in this implementation.");
    }

    #region Private Helper Methods - Invoice

    private void ComposeInvoiceHeader(IContainer container, InvoiceData data)
    {
        container.Row(row =>
        {
            // Company info (left)
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(data.CompanyName)
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().PaddingTop(5).Text(text =>
                {
                    text.Span(data.CompanyAddress).FontSize(9);
                });

                if (!string.IsNullOrEmpty(data.CompanyPhone))
                {
                    column.Item().Text($"Phone: {data.CompanyPhone}").FontSize(9);
                }

                if (!string.IsNullOrEmpty(data.CompanyEmail))
                {
                    column.Item().Text($"Email: {data.CompanyEmail}").FontSize(9);
                }
            });

            // Invoice info (right)
            row.RelativeItem().Column(column =>
            {
                column.Item().AlignRight().Text("INVOICE")
                    .FontSize(20)
                    .Bold()
                    .FontColor(Colors.Blue.Darken2);

                column.Item().AlignRight().Text($"# {data.InvoiceNumber}")
                    .FontSize(12)
                    .Bold();

                column.Item().AlignRight().PaddingTop(10).Text(text =>
                {
                    text.Span("Date: ").Bold();
                    text.Span(data.InvoiceDate.ToString("dd/MM/yyyy"));
                });

                column.Item().AlignRight().Text(text =>
                {
                    text.Span("Due Date: ").Bold();
                    text.Span(data.DueDate.ToString("dd/MM/yyyy"));
                });
            });
        });
    }

    private void ComposeInvoiceContent(IContainer container, InvoiceData data)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(20);

            // Customer info
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Bill To:").Bold().FontSize(11);
                    col.Item().PaddingTop(5).Text(data.CustomerName).FontSize(10);
                    
                    if (!string.IsNullOrEmpty(data.CustomerAddress))
                    {
                        col.Item().Text(data.CustomerAddress).FontSize(9);
                    }
                    
                    if (!string.IsNullOrEmpty(data.CustomerPhone))
                    {
                        col.Item().Text($"Phone: {data.CustomerPhone}").FontSize(9);
                    }
                    
                    if (!string.IsNullOrEmpty(data.CustomerEmail))
                    {
                        col.Item().Text($"Email: {data.CustomerEmail}").FontSize(9);
                    }
                });
            });

            // Items table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3); // Description
                    columns.RelativeColumn(1); // Quantity
                    columns.RelativeColumn(2); // Unit Price
                    columns.RelativeColumn(2); // Total
                });

                // Header
                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Description").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Quantity").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Unit Price").Bold();
                    header.Cell().Element(CellStyle).AlignRight().Text("Total").Bold();

                    static IContainer CellStyle(IContainer container)
                    {
                        return container
                            .BorderBottom(1)
                            .BorderColor(Colors.Grey.Medium)
                            .PaddingVertical(5);
                    }
                });

                // Items
                foreach (var item in data.Items)
                {
                    table.Cell().Element(CellStyle).Text(item.Description);
                    table.Cell().Element(CellStyle).AlignRight().Text(item.Quantity.ToString());
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.UnitPrice:N0} VND");
                    table.Cell().Element(CellStyle).AlignRight().Text($"{item.Total:N0} VND");

                    static IContainer CellStyle(IContainer container)
                    {
                        return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
                    }
                }
            });

            // Totals
            column.Item().AlignRight().PaddingTop(10).Column(col =>
            {
                col.Item().Row(row =>
                {
                    row.ConstantItem(120).Text("Subtotal:").Bold();
                    row.ConstantItem(120).AlignRight().Text($"{data.Subtotal:N0} VND");
                });

                col.Item().Row(row =>
                {
                    row.ConstantItem(120).Text($"Tax ({data.TaxRate * 100}%):").Bold();
                    row.ConstantItem(120).AlignRight().Text($"{data.TaxAmount:N0} VND");
                });

                col.Item().PaddingTop(5).BorderTop(2).BorderColor(Colors.Blue.Darken2);

                col.Item().PaddingTop(5).Row(row =>
                {
                    row.ConstantItem(120).Text("Total:").Bold().FontSize(12);
                    row.ConstantItem(120).AlignRight().Text($"{data.Total:N0} VND").Bold().FontSize(12);
                });
            });

            // Notes
            if (!string.IsNullOrEmpty(data.Notes))
            {
                column.Item().PaddingTop(20).Column(col =>
                {
                    col.Item().Text("Notes:").Bold();
                    col.Item().PaddingTop(5).Text(data.Notes).FontSize(9);
                });
            }

            // Payment terms
            if (!string.IsNullOrEmpty(data.PaymentTerms))
            {
                column.Item().PaddingTop(10).Column(col =>
                {
                    col.Item().Text("Payment Terms:").Bold();
                    col.Item().PaddingTop(5).Text(data.PaymentTerms).FontSize(9).Italic();
                });
            }
        });
    }

    #endregion

    #region Private Helper Methods - Report

    private void ComposeReportHeader(IContainer container, ReportData data, PdfOptions options)
    {
        container.Column(column =>
        {
            column.Item().AlignCenter().Text(data.Title)
                .FontSize(20)
                .Bold()
                .FontColor(Colors.Blue.Darken2);

            if (!string.IsNullOrEmpty(data.Subtitle))
            {
                column.Item().AlignCenter().Text(data.Subtitle)
                    .FontSize(12)
                    .Italic();
            }

            column.Item().AlignCenter().PaddingTop(5).Text(text =>
            {
                text.Span("Generated: ").FontSize(9);
                text.Span(data.GeneratedDate.ToString("dd/MM/yyyy HH:mm")).FontSize(9).Bold();
                text.Span(" by ").FontSize(9);
                text.Span(data.GeneratedBy).FontSize(9).Bold();
            });

            column.Item().PaddingTop(10).BorderBottom(2).BorderColor(Colors.Blue.Darken2);
        });
    }

    private void ComposeReportContent(IContainer container, ReportData data)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Spacing(15);

            // Summary
            if (!string.IsNullOrEmpty(data.Summary))
            {
                column.Item().Column(col =>
                {
                    col.Item().Text("Summary").FontSize(14).Bold();
                    col.Item().PaddingTop(5).Text(data.Summary).FontSize(10);
                });
            }

            // Sections
            foreach (var section in data.Sections)
            {
                column.Item().Column(col =>
                {
                    col.Item().Text(section.Title).FontSize(12).Bold();

                    if (!string.IsNullOrEmpty(section.Content))
                    {
                        col.Item().PaddingTop(5).Text(section.Content).FontSize(10);
                    }

                    if (section.ChartImage != null && section.ChartImage.Length > 0)
                    {
                        col.Item().PaddingTop(10).Image(section.ChartImage).FitWidth();
                    }

                    foreach (var table in section.Tables)
                    {
                        col.Item().PaddingTop(10).Column(tableCol =>
                        {
                            if (!string.IsNullOrEmpty(table.Title))
                            {
                                tableCol.Item().Text(table.Title).FontSize(11).Bold();
                            }

                            tableCol.Item().PaddingTop(5).Table(tbl =>
                            {
                                tbl.ColumnsDefinition(columns =>
                                {
                                    foreach (var _ in table.Headers)
                                    {
                                        columns.RelativeColumn();
                                    }
                                });

                                tbl.Header(header =>
                                {
                                    foreach (var h in table.Headers)
                                    {
                                        header.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Medium)
                                            .PaddingVertical(5)
                                            .Text(h)
                                            .Bold();
                                    }
                                });

                                foreach (var row in table.Rows)
                                {
                                    foreach (var cell in row)
                                    {
                                        tbl.Cell()
                                            .BorderBottom(1)
                                            .BorderColor(Colors.Grey.Lighten2)
                                            .PaddingVertical(5)
                                            .Text(cell);
                                    }
                                }
                            });
                        });
                    }
                });
            }
        });
    }

    #endregion

    #region Private Helper Methods - Footer

    private void ComposeFooter(IContainer container, PdfOptions options)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text(text =>
            {
                text.DefaultTextStyle(x => x.FontSize(8));
                if (!string.IsNullOrEmpty(options.FooterLeft))
                {
                    text.Span(options.FooterLeft);
                }
                else
                {
                    text.Span($"Generated: {DateTime.UtcNow:dd/MM/yyyy}");
                }
            });

            if (options.ShowPageNumbers)
            {
                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8));
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            }
            else if (!string.IsNullOrEmpty(options.FooterRight))
            {
                row.RelativeItem().AlignRight().Text(options.FooterRight).FontSize(8);
            }
        });
    }

    #endregion
}
