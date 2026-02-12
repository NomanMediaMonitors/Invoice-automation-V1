using System.Drawing;
using System.Text.RegularExpressions;
using Docnet.Core;
using Docnet.Core.Models;
using Invoice_automation_V1.ViewModels;
using InvoiceAutomation.Core.Configuration;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tesseract;
using SysImageFormat = System.Drawing.Imaging.ImageFormat;
using SysPixelFormat = System.Drawing.Imaging.PixelFormat;
using SysImageLockMode = System.Drawing.Imaging.ImageLockMode;

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
        string? extractedText = null;
        try
        {
            _logger.LogInformation("Processing invoice file: {FilePath}", filePath);

            // Extract text from the file
            extractedText = await ExtractTextAsync(filePath, fileType);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return new OcrResultViewModel
                {
                    Success = false,
                    ExtractedData = new OcrExtractedData { RawText = "[No text extracted]" },
                    ErrorMessage = "No text could be extracted from the file",
                    ConfidenceScore = 0
                };
            }

            _logger.LogInformation("OCR raw text ({Length} chars): {Text}",
                extractedText.Length,
                extractedText.Length > 500 ? extractedText.Substring(0, 500) + "..." : extractedText);

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

            // Still return ExtractedData with whatever raw text we got so it's persisted
            return new OcrResultViewModel
            {
                Success = false,
                ExtractedData = new OcrExtractedData
                {
                    RawText = extractedText ?? $"[OCR failed before text extraction: {ex.Message}]"
                },
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
                    using (var bitmap = new Bitmap(width, height, SysPixelFormat.Format32bppArgb))
                    {
                        var bitmapData = bitmap.LockBits(
                            new Rectangle(0, 0, width, height),
                            SysImageLockMode.WriteOnly,
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
                        bitmap.Save(ms, SysImageFormat.Png);
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
            // Allows alphanumeric plus common separators: - / . _
            var invoiceNumberPatterns = new[]
            {
                @"Invoice\s*No\.?\s*:?\s*([A-Z0-9][A-Z0-9\-/._]+)",
                @"Invoice\s*#?\s*:?\s*([A-Z0-9][A-Z0-9\-/._]+)",
                @"Inv\.?\s*#?\s*:?\s*([A-Z0-9][A-Z0-9\-/._]+)",
                @"Invoice\s+Number\s*:?\s*([A-Z0-9][A-Z0-9\-/._]+)",
                @"Bill\s+No\.?\s*:?\s*([A-Z0-9][A-Z0-9\-/._]+)",
                @"Ref(?:erence)?\s*(?:No\.?)?\s*:?\s*([A-Z0-9][A-Z0-9\-/._]+)"
            };

            foreach (var pattern in invoiceNumberPatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    data.InvoiceNumber = match.Groups[1].Value.Trim().TrimEnd('.');
                    break;
                }
            }

            // Extract Invoice Date - Multiple date formats including dd-MMM-yyyy
            var datePatterns = new[]
            {
                @"Invoice\s+Date\s*:?\s*(\d{1,2}[-/]\w{3,9}[-/]\d{2,4})",
                @"Invoice\s+Date\s*:?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
                @"(?:^|\s)Date\s*:?\s*(\d{1,2}[-/]\w{3,9}[-/]\d{2,4})",
                @"(?:^|\s)Date\s*:?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
                @"Dated\s*:?\s*(\d{1,2}[-/]\w{3,9}[-/]\d{2,4})",
                @"Dated\s*:?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
                @"(\d{1,2}[-/]\w{3,9}[-/]\d{2,4})",
                @"(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})"
            };

            foreach (var pattern in datePatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var invDate))
                {
                    data.InvoiceDate = invDate;
                    break;
                }
            }

            // Extract Due Date
            var dueDatePatterns = new[]
            {
                @"Due\s+Date\s*:?\s*(\d{1,2}[-/]\w{3,9}[-/]\d{2,4})",
                @"Due\s+Date\s*:?\s*(\d{1,2}[-/]\d{1,2}[-/]\d{2,4})",
                @"Payment\s+Due\s*:?\s*(\d{1,2}[-/]\w{3,9}[-/]\d{2,4})"
            };

            foreach (var pattern in dueDatePatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase);
                if (match.Success && DateTime.TryParse(match.Groups[1].Value, out var dDate))
                {
                    data.DueDate = dDate;
                    break;
                }
            }

            // Extract NTN Number (Pakistani Tax ID)
            var ntnMatch = Regex.Match(extractedText, @"NTN\s*(?:No\.?|Number)?\s*:?\s*([0-9][\d\-]+)", RegexOptions.IgnoreCase);

            // Extract Vendor Name - try multiple strategies
            var vendorPatterns = new[]
            {
                @"From\s*:?\s*\n?\s*([^\n]+)",
                @"Vendor\s*:?\s*([^\n]+)",
                @"Supplier\s*:?\s*([^\n]+)",
                @"Company\s*:?\s*([^\n]+)",
                @"M/[Ss]\.?\s*([^\n,]+)"
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

            // If no vendor name found via label, try to extract from the first meaningful line
            // (many invoices have the company/vendor name as the first or second line)
            if (string.IsNullOrWhiteSpace(data.VendorName))
            {
                var lines = extractedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines.Take(5))
                {
                    var trimmed = line.Trim();
                    // Look for a line that looks like a company name (has letters, not just numbers/dates)
                    if (trimmed.Length >= 3 && trimmed.Length <= 100 &&
                        Regex.IsMatch(trimmed, @"[A-Za-z]{2,}") &&
                        !Regex.IsMatch(trimmed, @"^\d") &&
                        !Regex.IsMatch(trimmed, @"(Date|Invoice|NTN|Tax|Page|Dear|Please)", RegexOptions.IgnoreCase))
                    {
                        data.VendorName = trimmed;
                        break;
                    }
                }
            }

            // Extract Totals with various formats including PKR with spaces
            var amountPatterns = new[]
            {
                @"Subtotal\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Sub[\s-]*Total\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)"
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

            // Tax extraction - also extract tax rate if present (e.g. "Tax 6%")
            var taxPatterns = new[]
            {
                @"Tax\s*\d*%?\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"GST\s*\d*%?\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"VAT\s*\d*%?\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"(?:Rs\.?|PKR)\s*([\d,]+\.?\d*)\s*(?:tax|gst)",
                @"Tax\s*(?:Rs\.?|PKR)\s*([\d,]+\.?\d*)"
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

            // Total amount - Grant Total, Grand Total, Total Amount, Net Amount
            var totalPatterns = new[]
            {
                @"Gr[ae]nt?\s+Total\s*(?:Amount)?\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Grand\s+Total\s*(?:Amount)?\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Total\s+Amount\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Net\s+(?:Amount|Total)\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Amount\s+(?:With|Inc(?:l|luding)?)\s+Tax\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"Amount\s+Due\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)",
                @"(?:^|\n)\s*Total\s*:?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)"
            };

            foreach (var pattern in totalPatterns)
            {
                var match = Regex.Match(extractedText, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out var total) && total > 0)
                {
                    // If we find multiple total amounts across pages, sum them
                    // (multi-page invoices like Rent & Ride have separate Grant Total per page)
                    if (data.TotalAmount.HasValue)
                    {
                        // Check if this is a different total on another page
                        var allMatches = Regex.Matches(extractedText, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        decimal sum = 0;
                        foreach (Match m in allMatches)
                        {
                            if (decimal.TryParse(m.Groups[1].Value.Replace(",", ""), out var val) && val > 0)
                            {
                                sum += val;
                            }
                        }
                        if (sum > data.TotalAmount.Value)
                        {
                            data.TotalAmount = sum;
                        }
                    }
                    else
                    {
                        // Sum all occurrences of this pattern (handles multi-page totals)
                        var allMatches = Regex.Matches(extractedText, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
                        decimal sum = 0;
                        foreach (Match m in allMatches)
                        {
                            if (decimal.TryParse(m.Groups[1].Value.Replace(",", ""), out var val) && val > 0)
                            {
                                sum += val;
                            }
                        }
                        data.TotalAmount = sum;
                    }
                    break;
                }
            }

            // Extract Line Items
            data.LineItems = ExtractLineItems(extractedText);

            // If we have line items but no subtotal, calculate it
            if (!data.SubTotal.HasValue && data.LineItems.Any())
            {
                data.SubTotal = data.LineItems.Sum(li => li.Amount);
            }

            _logger.LogInformation("Parsed invoice data: InvoiceNo={InvoiceNumber}, Date={Date}, Vendor={Vendor}, Total={Total}, LineItems={LineItemCount}",
                data.InvoiceNumber ?? "N/A",
                data.InvoiceDate?.ToString("yyyy-MM-dd") ?? "N/A",
                data.VendorName ?? "N/A",
                data.TotalAmount,
                data.LineItems.Count);
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
            var lines = extractedText.Split('\n');
            bool inItemsSection = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // Detect start of items section (table header row)
                if (Regex.IsMatch(trimmedLine, @"(S\.?\s*No|Item|Description|Product|Vehicle\s+Model)", RegexOptions.IgnoreCase)
                    && Regex.IsMatch(trimmedLine, @"(Amount|Price|Rent|Total|Rate)", RegexOptions.IgnoreCase))
                {
                    inItemsSection = true;
                    continue;
                }

                // Detect end of items section
                if (inItemsSection && Regex.IsMatch(trimmedLine, @"(Gr[ae]nt?\s+Total|Grand\s+Total|Subtotal|Sub\s+Total|Amount\s+Due)", RegexOptions.IgnoreCase))
                {
                    inItemsSection = false;
                    continue;
                }

                if (inItemsSection && !string.IsNullOrWhiteSpace(trimmedLine))
                {
                    // Strategy 1: Line has PKR amounts — extract description and last PKR amount as total
                    var pkrAmounts = Regex.Matches(trimmedLine, @"PKR\s*([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
                    if (pkrAmounts.Count > 0)
                    {
                        // Get the description (text before the first number or PKR)
                        var descMatch = Regex.Match(trimmedLine, @"^\d*\s*(.+?)(?:\s+\d+\s+PKR|\s+PKR|\s+\d[\d,]*\.\d)", RegexOptions.IgnoreCase);
                        var description = descMatch.Success ? descMatch.Groups[1].Value.Trim() : trimmedLine;

                        // Clean up description - remove leading serial numbers
                        description = Regex.Replace(description, @"^\d+\s+", "").Trim();

                        if (description.Length < 2) continue;

                        // Last PKR amount is typically the line total (Amount With Tax)
                        var lastAmount = pkrAmounts[pkrAmounts.Count - 1];
                        if (decimal.TryParse(lastAmount.Groups[1].Value.Replace(",", ""), out var amount) && amount > 0)
                        {
                            decimal quantity = 1;
                            decimal unitPrice = amount;

                            // Try to extract quantity (often a standalone number like Days)
                            var qtyMatch = Regex.Match(trimmedLine, @"\b(\d+)\s+PKR", RegexOptions.IgnoreCase);
                            if (qtyMatch.Success && decimal.TryParse(qtyMatch.Groups[1].Value, out var qty) && qty > 0 && qty < 10000)
                            {
                                quantity = qty;
                            }

                            // If there's an "Agreed Rent" or unit price PKR amount and a total, compute unit price
                            if (pkrAmounts.Count >= 2)
                            {
                                var firstPkr = pkrAmounts[0];
                                if (decimal.TryParse(firstPkr.Groups[1].Value.Replace(",", ""), out var firstAmount) && firstAmount > 0)
                                {
                                    unitPrice = firstAmount;
                                }
                            }

                            // Extract tax amount if present in line
                            decimal taxAmount = 0;
                            decimal taxRate = 0;
                            var taxMatch = Regex.Match(trimmedLine, @"Tax\s*(\d+)%?\s*(?:Rs\.?|PKR)?\s*([\d,]+\.?\d*)", RegexOptions.IgnoreCase);
                            if (!taxMatch.Success)
                            {
                                // Look for a PKR amount that looks like tax (second to last)
                                if (pkrAmounts.Count >= 3)
                                {
                                    var taxPkr = pkrAmounts[pkrAmounts.Count - 2];
                                    if (decimal.TryParse(taxPkr.Groups[1].Value.Replace(",", ""), out var possibleTax))
                                    {
                                        taxAmount = possibleTax;
                                    }
                                }
                            }
                            else
                            {
                                if (decimal.TryParse(taxMatch.Groups[1].Value, out var rate)) taxRate = rate;
                                if (decimal.TryParse(taxMatch.Groups[2].Value.Replace(",", ""), out var tAmt)) taxAmount = tAmt;
                            }

                            lineItems.Add(new OcrLineItem
                            {
                                Description = description,
                                Quantity = quantity,
                                UnitPrice = unitPrice,
                                Amount = amount,
                                ConfidenceScore = 80.0m
                            });
                            continue;
                        }
                    }

                    // Strategy 2: Simple format — Description then Amount (no PKR prefix)
                    // e.g. "Suzuki Alto AGS - BZZ286    Car Depreciation    PKR 14,470.00"
                    // or "Some description   14,470.00"
                    var simpleMatch = Regex.Match(trimmedLine, @"^\d*\s*(.+?)\s+(?:Rs\.?|PKR)?\s*([\d,]+\.\d{2})\s*$", RegexOptions.IgnoreCase);
                    if (simpleMatch.Success)
                    {
                        var description = simpleMatch.Groups[1].Value.Trim();
                        description = Regex.Replace(description, @"^\d+\s+", "").Trim();

                        if (description.Length >= 2 &&
                            !Regex.IsMatch(description, @"^(Item|Desc|Product|Qty|S\.?No|Vehicle)", RegexOptions.IgnoreCase) &&
                            decimal.TryParse(simpleMatch.Groups[2].Value.Replace(",", ""), out var amount) && amount > 0)
                        {
                            lineItems.Add(new OcrLineItem
                            {
                                Description = description,
                                Quantity = 1,
                                UnitPrice = amount,
                                Amount = amount,
                                ConfidenceScore = 70.0m
                            });
                            continue;
                        }
                    }

                    // Strategy 3: Original pattern — Description, Qty, UnitPrice, Amount (no currency prefix)
                    var tableMatch = Regex.Match(trimmedLine, @"([^\d]+?)\s+(\d+(?:\.\d+)?)\s+([\d,]+(?:\.\d+)?)\s+([\d,]+(?:\.\d+)?)");
                    if (tableMatch.Success && tableMatch.Groups.Count >= 5)
                    {
                        var description = tableMatch.Groups[1].Value.Trim();
                        description = Regex.Replace(description, @"^\d+\s+", "").Trim();

                        if (description.Length >= 3 &&
                            !Regex.IsMatch(description, @"^(Item|Desc|Product|Qty|S\.?No)", RegexOptions.IgnoreCase))
                        {
                            if (decimal.TryParse(tableMatch.Groups[2].Value, out var qty) &&
                                decimal.TryParse(tableMatch.Groups[3].Value.Replace(",", ""), out var unitPrice) &&
                                decimal.TryParse(tableMatch.Groups[4].Value.Replace(",", ""), out var amount) &&
                                amount > 0)
                            {
                                lineItems.Add(new OcrLineItem
                                {
                                    Description = description,
                                    Quantity = qty,
                                    UnitPrice = unitPrice,
                                    Amount = amount,
                                    ConfidenceScore = 75.0m
                                });
                            }
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
