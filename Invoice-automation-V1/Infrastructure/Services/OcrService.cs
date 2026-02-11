using System.Text.RegularExpressions;
using Invoice_automation_V1.ViewModels;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InvoiceAutomation.Infrastructure.Services;

public class OcrService : IOcrService
{
    private readonly ILogger<OcrService> _logger;

    public OcrService(ILogger<OcrService> logger)
    {
        _logger = logger;
    }

    public async Task<OcrResultViewModel> ProcessInvoiceAsync(string filePath, string fileType)
    {
        try
        {
            _logger.LogInformation("Processing invoice file: {FilePath}", filePath);

            // Extract text from the file
            var extractedText = await ExtractTextAsync(filePath, fileType);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return new OcrResultViewModel
                {
                    Success = false,
                    ErrorMessage = "No text could be extracted from the file",
                    ConfidenceScore = 0
                };
            }

            // Parse the extracted text
            var extractedData = ParseInvoiceData(extractedText);

            // Validate results
            bool isValid = ValidateOcrResults(extractedData);

            return new OcrResultViewModel
            {
                Success = isValid,
                ExtractedData = extractedData,
                ConfidenceScore = CalculateOverallConfidence(extractedData),
                ErrorMessage = isValid ? null : "OCR extraction completed but some required fields are missing"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice with OCR");
            return new OcrResultViewModel
            {
                Success = false,
                ErrorMessage = $"OCR processing failed: {ex.Message}",
                ConfidenceScore = 0
            };
        }
    }

    public async Task<string> ExtractTextAsync(string filePath, string fileType)
    {
        // TODO: Implement actual OCR integration (Tesseract, Google Vision API, Azure Computer Vision, etc.)
        // For now, return a mock extracted text for demonstration

        await Task.Delay(1000); // Simulate OCR processing time

        _logger.LogInformation("Simulating OCR text extraction from {FileType}", fileType);

        // Mock extracted text that looks like a typical invoice
        return @"
            INVOICE

            Invoice Number: INV-2024-001
            Invoice Date: 2024-12-15
            Due Date: 2025-01-15

            Bill To:
            ABC Company Ltd.
            123 Business Street
            Karachi, Pakistan

            From:
            XYZ Supplier Inc.
            456 Vendor Road
            Lahore, Pakistan

            ITEMS:
            Item Description              Qty    Unit Price    Amount
            Office Supplies               10     1,500.00      15,000.00
            Computer Equipment            2      50,000.00     100,000.00
            Software License              1      25,000.00     25,000.00

            Subtotal:                                         140,000.00
            Tax (17%):                                         23,800.00
            Total Amount:                                     163,800.00

            Payment Terms: Net 30 Days
            Bank Details: Standard Chartered Bank
            Account: 1234567890
        ";
    }

    public OcrExtractedData ParseInvoiceData(string extractedText)
    {
        var data = new OcrExtractedData
        {
            RawText = extractedText
        };

        try
        {
            // Extract Invoice Number
            var invoiceNumberMatch = Regex.Match(extractedText, @"Invoice\s+Number[:|\s]+([A-Z0-9-]+)", RegexOptions.IgnoreCase);
            if (invoiceNumberMatch.Success)
            {
                data.InvoiceNumber = invoiceNumberMatch.Groups[1].Value.Trim();
            }

            // Extract Invoice Date
            var invoiceDateMatch = Regex.Match(extractedText, @"Invoice\s+Date[:|\s]+(\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase);
            if (invoiceDateMatch.Success && DateTime.TryParse(invoiceDateMatch.Groups[1].Value, out var invDate))
            {
                data.InvoiceDate = invDate;
            }

            // Extract Due Date
            var dueDateMatch = Regex.Match(extractedText, @"Due\s+Date[:|\s]+(\d{4}-\d{2}-\d{2})", RegexOptions.IgnoreCase);
            if (dueDateMatch.Success && DateTime.TryParse(dueDateMatch.Groups[1].Value, out var dDate))
            {
                data.DueDate = dDate;
            }

            // Extract Vendor Name (from "From:" section)
            var vendorMatch = Regex.Match(extractedText, @"From:\s*\n\s*([^\n]+)", RegexOptions.IgnoreCase);
            if (vendorMatch.Success)
            {
                data.VendorName = vendorMatch.Groups[1].Value.Trim();
            }

            // Extract Totals
            var subtotalMatch = Regex.Match(extractedText, @"Subtotal[:|\s]+([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
            if (subtotalMatch.Success && decimal.TryParse(subtotalMatch.Groups[1].Value.Replace(",", ""), out var subtotal))
            {
                data.SubTotal = subtotal;
            }

            var taxMatch = Regex.Match(extractedText, @"Tax[^:]*[:|\s]+([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
            if (taxMatch.Success && decimal.TryParse(taxMatch.Groups[1].Value.Replace(",", ""), out var tax))
            {
                data.TaxAmount = tax;
            }

            var totalMatch = Regex.Match(extractedText, @"Total\s+Amount[:|\s]+([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
            if (totalMatch.Success && decimal.TryParse(totalMatch.Groups[1].Value.Replace(",", ""), out var total))
            {
                data.TotalAmount = total;
            }

            // Extract Line Items
            data.LineItems = ExtractLineItems(extractedText);

            _logger.LogInformation("Parsed invoice data: {InvoiceNumber}", data.InvoiceNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing OCR extracted text");
        }

        return data;
    }

    private List<OcrLineItem> ExtractLineItems(string extractedText)
    {
        var lineItems = new List<OcrLineItem>();

        try
        {
            // Find the items section
            var itemsMatch = Regex.Match(extractedText, @"ITEMS:(.*?)(?:Subtotal|Total)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (itemsMatch.Success)
            {
                var itemsSection = itemsMatch.Groups[1].Value;

                // Match line items (Description, Qty, Unit Price, Amount)
                var itemMatches = Regex.Matches(itemsSection, @"([^\n]+?)\s+(\d+)\s+([\d,]+\.?\d*)\s+([\d,]+\.?\d*)");

                foreach (Match match in itemMatches)
                {
                    if (match.Groups.Count >= 5)
                    {
                        var description = match.Groups[1].Value.Trim();

                        // Skip header row
                        if (description.Contains("Description") || description.Contains("Item") || description.Contains("Qty"))
                            continue;

                        if (decimal.TryParse(match.Groups[2].Value, out var qty) &&
                            decimal.TryParse(match.Groups[3].Value.Replace(",", ""), out var unitPrice) &&
                            decimal.TryParse(match.Groups[4].Value.Replace(",", ""), out var amount))
                        {
                            lineItems.Add(new OcrLineItem
                            {
                                Description = description,
                                Quantity = qty,
                                UnitPrice = unitPrice,
                                Amount = amount,
                                ConfidenceScore = 85.0m // Mock confidence score
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting line items");
        }

        return lineItems;
    }

    public bool ValidateOcrResults(OcrExtractedData data)
    {
        // Minimum required fields for a valid invoice
        return !string.IsNullOrWhiteSpace(data.InvoiceNumber) &&
               data.InvoiceDate.HasValue &&
               data.TotalAmount.HasValue &&
               data.TotalAmount.Value > 0;
    }

    private decimal CalculateOverallConfidence(OcrExtractedData data)
    {
        decimal confidence = 0;
        int fieldCount = 0;

        // Check each field and add to confidence
        if (!string.IsNullOrWhiteSpace(data.InvoiceNumber)) { confidence += 20; fieldCount++; }
        if (data.InvoiceDate.HasValue) { confidence += 15; fieldCount++; }
        if (data.DueDate.HasValue) { confidence += 10; fieldCount++; }
        if (!string.IsNullOrWhiteSpace(data.VendorName)) { confidence += 15; fieldCount++; }
        if (data.SubTotal.HasValue) { confidence += 10; fieldCount++; }
        if (data.TaxAmount.HasValue) { confidence += 10; fieldCount++; }
        if (data.TotalAmount.HasValue) { confidence += 20; fieldCount++; }

        // Add confidence for line items (up to 20 points)
        if (data.LineItems.Any())
        {
            var lineItemConfidence = Math.Min(20, data.LineItems.Count * 5);
            confidence += lineItemConfidence;
        }

        return Math.Min(confidence, 100);
    }
}
