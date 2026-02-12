using System.Drawing;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using Docnet.Core;
using Docnet.Core.Models;
using Invoice_automation_V1.ViewModels;
using InvoiceAutomation.Core.Configuration;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tesseract;

namespace InvoiceAutomation.Infrastructure.Services;

public class OcrService : IOcrService
{
    private readonly ILogger<OcrService> _logger;
    private readonly TesseractSettings _tesseractSettings;
    private readonly IWebHostEnvironment _environment;

    public OcrService(
        ILogger<OcrService> logger,
        IOptions<TesseractSettings> tesseractSettings,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _tesseractSettings = tesseractSettings.Value;
        _environment = environment;
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
        try
        {
            _logger.LogInformation("Extracting text from {FileType} file using Tesseract OCR", fileType);

            // Get the tessdata path
            var tessDataPath = Path.Combine(_environment.ContentRootPath, _tesseractSettings.DataPath);

            if (!Directory.Exists(tessDataPath))
            {
                _logger.LogError("Tessdata directory not found at: {TessDataPath}", tessDataPath);
                throw new DirectoryNotFoundException($"Tessdata directory not found at: {tessDataPath}");
            }

            _logger.LogInformation("Using tessdata path: {TessDataPath}", tessDataPath);

            string extractedText = string.Empty;

            // Handle different file types
            if (fileType.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                extractedText = await ExtractTextFromPdfAsync(filePath, tessDataPath);
            }
            else if (IsImageFile(fileType))
            {
                extractedText = await ExtractTextFromImageAsync(filePath, tessDataPath);
            }
            else
            {
                throw new NotSupportedException($"File type '{fileType}' is not supported for OCR");
            }

            _logger.LogInformation("Successfully extracted {Length} characters of text", extractedText?.Length ?? 0);

            return extractedText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting text from file");
            throw;
        }
    }

    private async Task<string> ExtractTextFromImageAsync(string imagePath, string tessDataPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                using var engine = new TesseractEngine(tessDataPath, _tesseractSettings.Language, EngineMode.Default);
                using var img = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(img);

                var text = page.GetText();
                var confidence = page.GetMeanConfidence();

                _logger.LogInformation("OCR completed with mean confidence: {Confidence:P}", confidence);

                return text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during Tesseract OCR processing for image");
                throw;
            }
        });
    }

    private async Task<string> ExtractTextFromPdfAsync(string pdfPath, string tessDataPath)
    {
        return await Task.Run(() =>
        {
            var extractedText = new System.Text.StringBuilder();

            try
            {
                using var library = DocLib.Instance;
                using var docReader = library.GetDocReader(pdfPath, new PageDimensions(1080, 1920));

                _logger.LogInformation("PDF has {PageCount} pages", docReader.GetPageCount());

                // Process each page (limit to first 10 pages for performance)
                int pagesToProcess = Math.Min(docReader.GetPageCount(), 10);

                for (int i = 0; i < pagesToProcess; i++)
                {
                    _logger.LogInformation("Processing PDF page {PageNumber}", i + 1);

                    using var pageReader = docReader.GetPageReader(i);
                    var rawBytes = pageReader.GetImage();
                    var width = pageReader.GetPageWidth();
                    var height = pageReader.GetPageHeight();

                    // Convert PDF page to image bytes
                    using var ms = new MemoryStream();
                    using (var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                    {
                        var bitmapData = bitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            ImageLockMode.WriteOnly,
                            bitmap.PixelFormat);

                        unsafe
                        {
                            var ptr = (byte*)bitmapData.Scan0;
                            for (int j = 0; j < rawBytes.Length; j++)
                            {
                                ptr[j] = rawBytes[j];
                            }
                        }

                        bitmap.UnlockBits(bitmapData);
                        bitmap.Save(ms, ImageFormat.Png);
                    }

                    ms.Position = 0;

                    // Perform OCR on the page image
                    using var engine = new TesseractEngine(tessDataPath, _tesseractSettings.Language, EngineMode.Default);
                    using var img = Pix.LoadFromMemory(ms.ToArray());
                    using var page = engine.Process(img);

                    var pageText = page.GetText();
                    extractedText.AppendLine(pageText);
                    extractedText.AppendLine(); // Add spacing between pages

                    _logger.LogInformation("Extracted {Length} characters from page {PageNumber}", pageText?.Length ?? 0, i + 1);
                }

                return extractedText.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during PDF OCR processing");
                throw;
            }
        });
    }

    private bool IsImageFile(string fileType)
    {
        var imageExtensions = new[] { "jpg", "jpeg", "png", "tif", "tiff", "bmp" };
        return imageExtensions.Contains(fileType.ToLowerInvariant());
    }

    public OcrExtractedData ParseInvoiceData(string extractedText)
    {
        var data = new OcrExtractedData
        {
            RawText = extractedText
        };

        try
        {
            // Extract Invoice Number - Multiple patterns
            var invoiceNumberPatterns = new[]
            {
                @"Invoice\s*#?\s*:?\s*([A-Z0-9-]+)",
                @"Inv\s*#?\s*:?\s*([A-Z0-9-]+)",
                @"Invoice\s+Number\s*:?\s*([A-Z0-9-]+)",
                @"Bill\s+No\s*:?\s*([A-Z0-9-]+)"
            };

            foreach (var pattern in invoiceNumberPatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    data.InvoiceNumber = match.Groups[1].Value.Trim();
                    break;
                }
            }

            // Extract Invoice Date - Multiple date formats
            var datePatterns = new[]
            {
                @"Invoice\s+Date\s*:?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
                @"Date\s*:?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
                @"Dated\s*:?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
                @"(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})"
            };

            foreach (var pattern in datePatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var invDate))
                {
                    data.InvoiceDate = invDate;
                    break;
                }
            }

            // Extract Due Date
            var dueDateMatch = Regex.Match(extractedText, @"Due\s+Date\s*:?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})", RegexOptions.IgnoreCase);
            if (dueDateMatch.Success && DateTime.TryParse(dueDateMatch.Groups[1].Value, out var dDate))
            {
                data.DueDate = dDate;
            }

            // Extract Vendor Name
            var vendorPatterns = new[]
            {
                @"From\s*:?\s*\n?\s*([^\n]+)",
                @"Vendor\s*:?\s*([^\n]+)",
                @"Supplier\s*:?\s*([^\n]+)"
            };

            foreach (var pattern in vendorPatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    data.VendorName = match.Groups[1].Value.Trim();
                    break;
                }
            }

            // Extract Totals with various formats
            var amountPatterns = new[]
            {
                @"Subtotal\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Sub\s+Total\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Amount\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)"
            };

            foreach (var pattern in amountPatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var subtotal))
                {
                    data.SubTotal = subtotal;
                    break;
                }
            }

            var taxPatterns = new[]
            {
                @"Tax\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"GST\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"VAT\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)"
            };

            foreach (var pattern in taxPatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var tax))
                {
                    data.TaxAmount = tax;
                    break;
                }
            }

            var totalPatterns = new[]
            {
                @"Total\s+Amount\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Grand\s+Total\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Total\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Amount\s+Due\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)"
            };

            foreach (var pattern in totalPatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var total))
                {
                    data.TotalAmount = total;
                    break;
                }
            }

            // Extract Line Items
            data.LineItems = ExtractLineItems(extractedText);

            _logger.LogInformation("Parsed invoice data: {InvoiceNumber}, Total: {Total}",
                data.InvoiceNumber ?? "N/A", data.TotalAmount);
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
            // Try to find table-like structure with items
            var lines = extractedText.Split('\n');
            bool inItemsSection = false;

            foreach (var line in lines)
            {
                // Detect start of items section
                if (Regex.IsMatch(line, @"(Item|Description|Product|S\.?No)", RegexOptions.IgnoreCase))
                {
                    inItemsSection = true;
                    continue;
                }

                // Detect end of items section
                if (Regex.IsMatch(line, @"(Subtotal|Total|Tax|Amount Due)", RegexOptions.IgnoreCase))
                {
                    inItemsSection = false;
                    break;
                }

                if (inItemsSection && !string.IsNullOrWhiteSpace(line))
                {
                    // Try to extract: Description, Quantity, Unit Price, Amount
                    var match = Regex.Match(line, @"([^\d]+?)\s+(\d+(?:\.\d+)?)\s+([\d,]+(?:\.\d+)?)\s+([\d,]+(?:\.\d+)?)");

                    if (match.Success && match.Groups.Count >= 5)
                    {
                        var description = match.Groups[1].Value.Trim();

                        // Skip if it looks like a header
                        if (description.Length < 3 || Regex.IsMatch(description, @"^(Item|Desc|Product|Qty)", RegexOptions.IgnoreCase))
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
                                ConfidenceScore = 75.0m // Base confidence for extracted items
                            });
                        }
                    }
                }
            }

            _logger.LogInformation("Extracted {Count} line items", lineItems.Count);
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
