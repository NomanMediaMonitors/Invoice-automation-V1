using Invoice_automation_V1.ViewModels;

namespace InvoiceAutomation.Core.Interfaces;

public interface IOcrService
{
    /// <summary>
    /// Process an invoice file and extract data using OCR
    /// </summary>
    Task<OcrResultViewModel> ProcessInvoiceAsync(string filePath, string fileType);

    /// <summary>
    /// Extract text from an image or PDF
    /// </summary>
    Task<string> ExtractTextAsync(string filePath, string fileType);

    /// <summary>
    /// Parse extracted text into structured invoice data
    /// </summary>
    OcrExtractedData ParseInvoiceData(string extractedText);

    /// <summary>
    /// Validate OCR results
    /// </summary>
    bool ValidateOcrResults(OcrExtractedData data);
}
