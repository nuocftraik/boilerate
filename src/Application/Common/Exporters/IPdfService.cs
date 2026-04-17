using Boilerate.Application.Common.Interfaces;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Boilerate.Application.Common.Exporters;

/// <summary>
/// Service for generating PDF documents
/// </summary>
public interface IPdfService : ITransientService
{
    /// <summary>
    /// Tạo file PDF Hóa đơn
    /// </summary>
    Task<byte[]> CreateInvoicePdfAsync(
        InvoiceData data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Tạo file PDF Báo cáo tổng hợp
    /// </summary>
    Task<byte[]> CreateReportPdfAsync(
        ReportData data,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate custom PDF from HTML
    /// </summary>
    /// <param name="html">HTML content</param>
    /// <param name="options">PDF options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF file as byte array</returns>
    Task<byte[]> GenerateFromHtmlAsync(
        string html,
        PdfOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Merge multiple PDF files
    /// </summary>
    /// <param name="pdfFiles">List of PDF files as byte arrays</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Merged PDF file as byte array</returns>
    Task<byte[]> MergePdfsAsync(
        List<byte[]> pdfFiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add watermark to existing PDF
    /// </summary>
    /// <param name="pdfFile">PDF file as byte array</param>
    /// <param name="watermarkText">Watermark text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>PDF with watermark as byte array</returns>
    Task<byte[]> AddWatermarkAsync(
        byte[] pdfFile,
        string watermarkText,
        CancellationToken cancellationToken = default);
}
