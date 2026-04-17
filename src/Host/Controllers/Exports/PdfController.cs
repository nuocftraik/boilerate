using Boilerate.Application.Common.Exporters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Boilerate.Host.Controllers.Exports;

/// <summary>
/// PDF export endpoints
/// </summary>
public class PdfController : BaseApiController
{
    private readonly IPdfService _pdfService;
    private readonly ILogger<PdfController> _logger;

    public PdfController(
        IPdfService pdfService,
        ILogger<PdfController> logger)
    {
        _pdfService = pdfService;
        _logger = logger;
    }

    /// <summary>
    /// Generate sample invoice PDF
    /// </summary>
    /// <returns>PDF file</returns>
    [HttpGet("invoice/sample")]
    [AllowAnonymous] // Testing only
    public async Task<IActionResult> GenerateSampleInvoice(CancellationToken cancellationToken)
    {
        var invoiceData = new InvoiceData
        {
            InvoiceNumber = "INV-2024-001",
            InvoiceDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            
            CompanyName = "Boilerate Enterprise",
            CompanyAddress = "123 Tech Avenue, Silicon Valley, CA",
            CompanyPhone = "+1 555 123 4567",
            CompanyEmail = "billing@boilerate.com",
        
            CustomerName = "John Doe",
            CustomerAddress = "456 Main St, New York, NY",
            CustomerPhone = "+1 555 987 6543",
            CustomerEmail = "john.doe@example.com",
    
            Items = new List<InvoiceItem>
            {
                new("Standard Subscription (Annual)", 1, 5000000),
                new("Advanced Analytics Module", 1, 2500000),
                new("Extra User License", 5, 200000),
                new("Setup & Configuration", 1, 1000000)
            },
            
            TaxRate = 0.1m,
            Notes = "Thank you for using Boilerate!",
            PaymentTerms = "Please pay within 30 days via bank transfer."
        };

        var options = new PdfOptions
        {
            Title = $"Invoice {invoiceData.InvoiceNumber}",
            Author = "Boilerate System"
        };

        var pdfBytes = await _pdfService.CreateInvoicePdfAsync(invoiceData, options, cancellationToken);

        return File(pdfBytes, "application/pdf", $"Invoice_{invoiceData.InvoiceNumber}.pdf");
    }

    /// <summary>
    /// Generate sample report PDF
    /// </summary>
    /// <returns>PDF file</returns>
    [HttpGet("report/sample")]
    [AllowAnonymous] // Testing only
    public async Task<IActionResult> GenerateSampleReport(CancellationToken cancellationToken)
    {
        var reportData = new ReportData
        {
            Title = "Hệ thống Báo cáo Tổng quan Q1 2024",
            Subtitle = "Phân tích dữ liệu kinh doanh",
            GeneratedDate = DateTime.UtcNow,
            GeneratedBy = "Administrator",
            
            Summary = "Báo cáo này liệt kê các hoạt động kinh doanh quan trọng trong Quý 1 năm 2024. Tổng doanh thu đạt mức tăng trưởng 15% so với cùng kỳ năm ngoái.",
            
            Sections = new List<ReportSection>
            {
                new()
                {
                    Title = "1. Tổng quan Doanh thu",
                    Content = "Phần này liệt kê chi tiết doanh thu theo từng tháng.",
                    Tables = new List<ReportTable>
                    {
                        new()
                        {
                            Title = "Bảng Doanh thu theo Tháng",
                            Headers = new List<string> { "Tháng", "Mục tiêu (VND)", "Thực tế (VND)", "Tỉ lệ (%)" },
                            Rows = new List<List<string>>
                            {
                                new() { "Tháng 1", "500,000,000", "550,000,000", "110%" },
                                new() { "Tháng 2", "500,000,000", "480,000,000", "96%" },
                                new() { "Tháng 3", "600,000,000", "720,000,000", "120%" }
                            }
                        }
                    }
                },
                new()
                {
                    Title = "2. Phân tích Khách hàng",
                    Content = "Thông tin về các phân khúc khách hàng chính trong kỳ báo cáo.",
                    Tables = new List<ReportTable>
                    {
                        new()
                        {
                            Title = "Phân khúc Khách hàng",
                            Headers = new List<string> { "Phân khúc", "Số lượng", "Tỉ trọng" },
                            Rows = new List<List<string>>
                            {
                                new() { "Doanh nghiệp lớn", "50", "45%" },
                                new() { "Doanh nghiệp vừa và nhỏ", "200", "35%" },
                                new() { "Cá nhân", "1050", "20%" }
                            }
                        }
                    }
                }
            }
        };

        var options = new PdfOptions
        {
            Title = reportData.Title,
            Author = "Boilerate System",
            FooterLeft = "Tài liệu nội bộ - Boilerate"
        };

        var pdfBytes = await _pdfService.CreateReportPdfAsync(reportData, options, cancellationToken);

        return File(pdfBytes, "application/pdf", "SampleReport.pdf");
    }
}
