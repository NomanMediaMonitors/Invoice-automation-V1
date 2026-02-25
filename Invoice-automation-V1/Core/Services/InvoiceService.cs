using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.Infrastructure.Data;
using Invoice_automation_V1.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvoiceAutomation.Core.Services;

public class InvoiceService : IInvoiceService
{
    private readonly ApplicationDbContext _context;
    private readonly IOcrService _ocrService;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(
        ApplicationDbContext context,
        IOcrService ocrService,
        ILogger<InvoiceService> logger)
    {
        _context = context;
        _ocrService = ocrService;
        _logger = logger;
    }

    public async Task<(bool Success, string Message, Guid? InvoiceId)> CreateInvoiceAsync(CreateInvoiceViewModel model, Guid userId)
    {
        try
        {
            // Verify user has access to the company
            var hasAccess = await _context.UserCompanies
                .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == model.CompanyId);

            if (!hasAccess)
            {
                return (false, "You don't have access to this company", null);
            }

            // Generate a temporary invoice number if not provided (OCR will update it later)
            var invoiceNumber = model.InvoiceNumber;
            if (string.IsNullOrWhiteSpace(invoiceNumber))
            {
                invoiceNumber = $"DRAFT-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            }

            // Check if invoice number already exists for this company
            var exists = await _context.Invoices
                .AnyAsync(i => i.CompanyId == model.CompanyId && i.InvoiceNumber == invoiceNumber);

            if (exists)
            {
                return (false, $"Invoice number {invoiceNumber} already exists for this company", null);
            }

            // Create invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                CompanyId = model.CompanyId,
                VendorId = model.VendorId,
                InvoiceNumber = invoiceNumber,
                InvoiceDate = model.InvoiceDate,
                DueDate = model.DueDate ?? model.InvoiceDate.AddDays(30),
                Description = model.Description,
                Notes = model.Notes,
                Status = InvoiceStatus.Draft,
                Currency = "PKR",
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Add line items if provided
            int lineNumber = 1;
            foreach (var lineItem in model.LineItems)
            {
                var invoiceLineItem = new InvoiceLineItem
                {
                    Id = Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    LineNumber = lineNumber++,
                    Description = lineItem.Description,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    Amount = lineItem.Quantity * lineItem.UnitPrice,
                    TaxRate = lineItem.TaxRate,
                    TaxAmount = (lineItem.Quantity * lineItem.UnitPrice) * (lineItem.TaxRate / 100),
                    ChartOfAccountId = lineItem.ChartOfAccountId,
                    AccountCode = lineItem.AccountCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                invoiceLineItem.TotalAmount = invoiceLineItem.Amount + invoiceLineItem.TaxAmount;
                invoice.LineItems.Add(invoiceLineItem);
            }

            // Calculate totals
            invoice.SubTotal = invoice.LineItems.Sum(li => li.Amount);
            invoice.TaxAmount = invoice.LineItems.Sum(li => li.TaxAmount);
            invoice.TotalAmount = invoice.SubTotal + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceNumber} created successfully", invoice.InvoiceNumber);

            return (true, "Invoice created successfully", invoice.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return (false, $"Error creating invoice: {ex.Message}", null);
        }
    }

    public async Task<(bool Success, string Message)> UpdateInvoiceAsync(EditInvoiceViewModel model, Guid userId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == model.Id);

            if (invoice == null)
            {
                return (false, "Invoice not found");
            }

            // Check if user has access
            if (!await CanUserAccessInvoiceAsync(invoice.Id, userId))
            {
                return (false, "You don't have permission to edit this invoice");
            }

            // Don't allow editing if invoice is paid
            if (invoice.Status == InvoiceStatus.Paid)
            {
                return (false, "Cannot edit a paid invoice");
            }

            // Update invoice fields
            invoice.VendorId = model.VendorId;
            invoice.InvoiceNumber = model.InvoiceNumber;
            invoice.InvoiceDate = model.InvoiceDate;
            invoice.DueDate = model.DueDate;
            invoice.Description = model.Description;
            invoice.Notes = model.Notes;
            invoice.UpdatedAt = DateTime.UtcNow;
            invoice.UpdatedBy = userId;

            // Update line items
            // Remove existing line items
            _context.InvoiceLineItems.RemoveRange(invoice.LineItems);

            // Add updated line items
            int lineNumber = 1;
            foreach (var lineItem in model.LineItems)
            {
                var invoiceLineItem = new InvoiceLineItem
                {
                    Id = lineItem.Id ?? Guid.NewGuid(),
                    InvoiceId = invoice.Id,
                    LineNumber = lineNumber++,
                    Description = lineItem.Description,
                    Quantity = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice,
                    Amount = lineItem.Quantity * lineItem.UnitPrice,
                    TaxRate = lineItem.TaxRate,
                    TaxAmount = (lineItem.Quantity * lineItem.UnitPrice) * (lineItem.TaxRate / 100),
                    ChartOfAccountId = lineItem.ChartOfAccountId,
                    AccountCode = lineItem.AccountCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                invoiceLineItem.TotalAmount = invoiceLineItem.Amount + invoiceLineItem.TaxAmount;
                invoice.LineItems.Add(invoiceLineItem);
            }

            // Recalculate totals
            invoice.SubTotal = invoice.LineItems.Sum(li => li.Amount);
            invoice.TaxAmount = invoice.LineItems.Sum(li => li.TaxAmount);
            invoice.TotalAmount = invoice.SubTotal + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} updated successfully", invoice.Id);

            return (true, "Invoice updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice");
            return (false, $"Error updating invoice: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeleteInvoiceAsync(Guid invoiceId, Guid userId)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);

            if (invoice == null)
            {
                return (false, "Invoice not found");
            }

            if (!await CanUserAccessInvoiceAsync(invoiceId, userId))
            {
                return (false, "You don't have permission to delete this invoice");
            }

            // Don't allow deleting paid invoices
            if (invoice.Status == InvoiceStatus.Paid)
            {
                return (false, "Cannot delete a paid invoice");
            }

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} deleted successfully", invoiceId);

            return (true, "Invoice deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice");
            return (false, $"Error deleting invoice: {ex.Message}");
        }
    }

    public async Task<InvoiceDetailsViewModel?> GetInvoiceDetailsAsync(Guid invoiceId, Guid userId)
    {
        if (!await CanUserAccessInvoiceAsync(invoiceId, userId))
        {
            return null;
        }

        var invoice = await _context.Invoices
            .Include(i => i.Company)
            .Include(i => i.Vendor)
            .Include(i => i.AdvanceTaxAccount)
            .Include(i => i.SalesTaxInputAccount)
            .Include(i => i.LineItems)
                .ThenInclude(li => li.ChartOfAccount)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null)
        {
            return null;
        }

        // Get user names for audit fields
        var createdByUser = await _context.Users.FindAsync(invoice.CreatedBy);
        var approvedByUser = invoice.ApprovedBy.HasValue ? await _context.Users.FindAsync(invoice.ApprovedBy.Value) : null;
        var paidByUser = invoice.PaidBy.HasValue ? await _context.Users.FindAsync(invoice.PaidBy.Value) : null;
        var postedByUser = invoice.PostedToGLBy.HasValue ? await _context.Users.FindAsync(invoice.PostedToGLBy.Value) : null;

        // Load vendor template visibility flags
        bool hasDueDate = true, hasDescription = true, hasLineItems = true, hasTaxRate = true, hasSubTotal = true;
        bool hasAdvanceTaxAccount = true, hasSalesTaxInputAccount = true;
        if (invoice.VendorId.HasValue)
        {
            var vendorTemplate = await _context.VendorInvoiceTemplates
                .FirstOrDefaultAsync(t => t.VendorId == invoice.VendorId.Value && t.IsActive);
            if (vendorTemplate != null)
            {
                hasDueDate = vendorTemplate.HasDueDate;
                hasDescription = vendorTemplate.HasDescription;
                hasLineItems = vendorTemplate.HasLineItems;
                hasTaxRate = vendorTemplate.HasTaxRate;
                hasSubTotal = vendorTemplate.HasSubTotal;
                hasAdvanceTaxAccount = vendorTemplate.HasAdvanceTaxAccount;
                hasSalesTaxInputAccount = vendorTemplate.HasSalesTaxInputAccount;
            }
        }

        var viewModel = new InvoiceDetailsViewModel
        {
            Id = invoice.Id,
            CompanyId = invoice.CompanyId,
            CompanyName = invoice.Company?.Name ?? "",
            VendorId = invoice.VendorId,
            VendorName = invoice.Vendor?.Name ?? "",
            VendorEmail = invoice.Vendor?.Email ?? "",
            VendorPhone = invoice.Vendor?.Phone,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            TotalAmount = invoice.TotalAmount,
            Currency = invoice.Currency,
            Status = invoice.Status.ToString(),
            Description = invoice.Description,
            Notes = invoice.Notes,
            OriginalFileName = invoice.OriginalFileName,
            FileUrl = invoice.FileUrl,
            FileType = invoice.FileType,
            FileSize = invoice.FileSize,
            IsOcrProcessed = invoice.IsOcrProcessed,
            OcrProcessedAt = invoice.OcrProcessedAt,
            OcrConfidenceScore = invoice.OcrConfidenceScore,
            OcrErrorMessage = invoice.OcrErrorMessage,
            // GL Account assignments
            AdvanceTaxAccountId = invoice.AdvanceTaxAccountId,
            AdvanceTaxAccountName = invoice.AdvanceTaxAccount?.DisplayName,
            AdvanceTaxAmount = invoice.AdvanceTaxAmount,
            SalesTaxInputAccountId = invoice.SalesTaxInputAccountId,
            SalesTaxInputAccountName = invoice.SalesTaxInputAccount?.DisplayName,
            SalesTaxInputAmount = invoice.SalesTaxInputAmount,
            // GL Posting
            IsPostedToGL = invoice.IsPostedToGL,
            PostedToGLAt = invoice.PostedToGLAt,
            PostedToGLByName = postedByUser?.FullName,
            // Vendor template visibility
            HasDueDate = hasDueDate,
            HasDescription = hasDescription,
            HasLineItems = hasLineItems,
            HasTaxRate = hasTaxRate,
            HasSubTotal = hasSubTotal,
            HasAdvanceTaxAccount = hasAdvanceTaxAccount,
            HasSalesTaxInputAccount = hasSalesTaxInputAccount,
            // Approval/Payment
            ApprovedByName = approvedByUser?.FullName,
            ApprovedAt = invoice.ApprovedAt,
            ApprovalNotes = invoice.ApprovalNotes,
            PaidAt = invoice.PaidAt,
            PaymentReference = invoice.PaymentReference,
            PaidByName = paidByUser?.FullName,
            CreatedByName = createdByUser?.FullName ?? "",
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            LineItems = invoice.LineItems.OrderBy(li => li.LineNumber).Select(li => new InvoiceLineItemViewModel
            {
                Id = li.Id,
                LineNumber = li.LineNumber,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Amount = li.Amount,
                TaxRate = li.TaxRate,
                TaxAmount = li.TaxAmount,
                TotalAmount = li.TotalAmount,
                ChartOfAccountId = li.ChartOfAccountId,
                AccountCode = li.AccountCode,
                AccountName = li.ChartOfAccount?.Name,
                IsOcrExtracted = li.IsOcrExtracted,
                OcrConfidenceScore = li.OcrConfidenceScore
            }).ToList()
        };

        // Calculate overdue status
        if (invoice.DueDate.HasValue && invoice.Status != InvoiceStatus.Paid)
        {
            viewModel.IsOverdue = invoice.DueDate.Value < DateTime.Today;
            viewModel.DaysUntilDue = (invoice.DueDate.Value - DateTime.Today).Days;
        }

        return viewModel;
    }

    public async Task<InvoiceListViewModel> GetInvoicesAsync(Guid companyId, int pageNumber, int pageSize, string? statusFilter, string? searchQuery)
    {
        var query = _context.Invoices
            .Include(i => i.Vendor)
            .Where(i => i.CompanyId == companyId);

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(statusFilter) && Enum.TryParse<InvoiceStatus>(statusFilter, out var status))
        {
            query = query.Where(i => i.Status == status);
        }

        // Apply search query
        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query = query.Where(i =>
                i.InvoiceNumber.Contains(searchQuery) ||
                (i.Vendor != null && i.Vendor.Name.Contains(searchQuery)));
        }

        var totalCount = await query.CountAsync();

        var invoices = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceListItemViewModel
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                DueDate = i.DueDate,
                VendorName = i.Vendor != null ? i.Vendor.Name : "",
                TotalAmount = i.TotalAmount,
                Currency = i.Currency,
                Status = i.Status.ToString(),
                IsOcrProcessed = i.IsOcrProcessed,
                IsOverdue = i.DueDate.HasValue && i.DueDate.Value < DateTime.Today && i.Status != InvoiceStatus.Paid
            })
            .ToListAsync();

        // Calculate DaysUntilDue in memory after fetching from database
        foreach (var invoice in invoices)
        {
            if (invoice.DueDate.HasValue)
            {
                invoice.DaysUntilDue = (invoice.DueDate.Value - DateTime.Today).Days;
            }
        }

        return new InvoiceListViewModel
        {
            Invoices = invoices,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            StatusFilter = statusFilter,
            SearchQuery = searchQuery
        };
    }

    public async Task<(bool Success, string Message)> ApproveInvoiceAsync(Guid invoiceId, Guid userId, string? approvalNotes)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);

            if (invoice == null)
            {
                return (false, "Invoice not found");
            }

            if (!await CanUserAccessInvoiceAsync(invoiceId, userId))
            {
                return (false, "You don't have permission to approve this invoice");
            }

            if (invoice.Status != InvoiceStatus.PendingApproval && invoice.Status != InvoiceStatus.Draft)
            {
                return (false, $"Cannot approve invoice with status: {invoice.Status}");
            }

            invoice.Status = InvoiceStatus.Approved;
            invoice.ApprovedBy = userId;
            invoice.ApprovedAt = DateTime.UtcNow;
            invoice.ApprovalNotes = approvalNotes;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} approved by user {UserId}", invoiceId, userId);

            return (true, "Invoice approved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving invoice");
            return (false, $"Error approving invoice: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> RejectInvoiceAsync(Guid invoiceId, Guid userId, string? rejectionNotes)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);

            if (invoice == null)
            {
                return (false, "Invoice not found");
            }

            if (!await CanUserAccessInvoiceAsync(invoiceId, userId))
            {
                return (false, "You don't have permission to reject this invoice");
            }

            invoice.Status = InvoiceStatus.Rejected;
            invoice.ApprovedBy = userId;
            invoice.ApprovedAt = DateTime.UtcNow;
            invoice.ApprovalNotes = rejectionNotes;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} rejected by user {UserId}", invoiceId, userId);

            return (true, "Invoice rejected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting invoice");
            return (false, $"Error rejecting invoice: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> MarkAsPaidAsync(Guid invoiceId, Guid userId, DateTime paymentDate, string paymentReference)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(invoiceId);

            if (invoice == null)
            {
                return (false, "Invoice not found");
            }

            if (!await CanUserAccessInvoiceAsync(invoiceId, userId))
            {
                return (false, "You don't have permission to mark this invoice as paid");
            }

            if (invoice.Status != InvoiceStatus.Approved)
            {
                return (false, "Only approved invoices can be marked as paid");
            }

            invoice.Status = InvoiceStatus.Paid;
            invoice.PaidAt = paymentDate;
            invoice.PaymentReference = paymentReference;
            invoice.PaidBy = userId;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} marked as paid", invoiceId);

            return (true, "Invoice marked as paid successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice as paid");
            return (false, $"Error marking invoice as paid: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ProcessOcrAsync(Guid invoiceId)
    {
        Invoice? invoice = null;
        try
        {
            invoice = await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                return (false, "Invoice not found");
            }

            if (string.IsNullOrWhiteSpace(invoice.FileStoragePath))
            {
                return (false, "No file found for OCR processing");
            }

            // Process with OCR service
            var ocrResult = await _ocrService.ProcessInvoiceAsync(invoice.FileStoragePath, invoice.FileType ?? "");

            invoice.IsOcrProcessed = true;
            invoice.OcrProcessedAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            if (ocrResult.ExtractedData != null)
            {
                invoice.OcrConfidenceScore = ocrResult.ConfidenceScore;
                invoice.OcrRawData = ocrResult.ExtractedData.RawText;
                invoice.OcrErrorMessage = ocrResult.ErrorMessage;

                // Always update invoice number from OCR if extracted
                if (!string.IsNullOrWhiteSpace(ocrResult.ExtractedData.InvoiceNumber))
                {
                    invoice.InvoiceNumber = ocrResult.ExtractedData.InvoiceNumber;
                }

                // Always update dates from OCR if extracted
                if (ocrResult.ExtractedData.InvoiceDate.HasValue)
                {
                    invoice.InvoiceDate = ocrResult.ExtractedData.InvoiceDate.Value;
                }

                if (ocrResult.ExtractedData.DueDate.HasValue)
                {
                    invoice.DueDate = ocrResult.ExtractedData.DueDate.Value;
                }

                // Use the pre-selected vendor's template for OCR matching
                VendorInvoiceTemplate? vendorTemplate = null;
                if (invoice.VendorId.HasValue && invoice.VendorId.Value != Guid.Empty)
                {
                    // Vendor was selected at upload time - use their template directly
                    vendorTemplate = await _context.VendorInvoiceTemplates
                        .FirstOrDefaultAsync(t => t.VendorId == invoice.VendorId.Value && t.IsActive);

                    if (vendorTemplate != null)
                    {
                        _logger.LogInformation("Using vendor template '{TemplateName}' for OCR processing (vendor pre-selected)",
                            vendorTemplate.TemplateName);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(ocrResult.ExtractedData.VendorName))
                {
                    // Fallback: try to match vendor by name from OCR text
                    var vendorName = ocrResult.ExtractedData.VendorName;
                    var matchedVendor = await _context.Vendors
                        .Where(v => v.CompanyId == invoice.CompanyId && v.IsActive)
                        .ToListAsync();

                    var matched = matchedVendor.FirstOrDefault(v =>
                        v.Name.Contains(vendorName, StringComparison.OrdinalIgnoreCase) ||
                        vendorName.Contains(v.Name, StringComparison.OrdinalIgnoreCase));

                    if (matched != null)
                    {
                        invoice.VendorId = matched.Id;
                        _logger.LogInformation("Matched vendor '{VendorName}' (Id: {VendorId}) from OCR text",
                            matched.Name, matched.Id);

                        vendorTemplate = await _context.VendorInvoiceTemplates
                            .FirstOrDefaultAsync(t => t.VendorId == matched.Id && t.IsActive);

                        if (vendorTemplate != null)
                        {
                            _logger.LogInformation("Using vendor template '{TemplateName}' for OCR processing",
                                vendorTemplate.TemplateName);
                        }
                    }
                }

                // Apply template defaults
                var defaultTaxRate = vendorTemplate?.DefaultTaxRate ?? 0;
                var defaultChartOfAccountId = vendorTemplate?.DefaultChartOfAccountId;
                string? defaultAccountCode = null;
                if (defaultChartOfAccountId.HasValue)
                {
                    var defaultAccount = await _context.ChartOfAccounts.FindAsync(defaultChartOfAccountId.Value);
                    defaultAccountCode = defaultAccount?.Code;
                }

                // Auto-populate GL accounts from vendor template defaults
                if (vendorTemplate != null)
                {
                    if (vendorTemplate.HasAdvanceTaxAccount && vendorTemplate.DefaultAdvanceTaxAccountId.HasValue && !invoice.AdvanceTaxAccountId.HasValue)
                        invoice.AdvanceTaxAccountId = vendorTemplate.DefaultAdvanceTaxAccountId;
                    if (vendorTemplate.HasSalesTaxInputAccount && vendorTemplate.DefaultSalesTaxInputAccountId.HasValue && !invoice.SalesTaxInputAccountId.HasValue)
                        invoice.SalesTaxInputAccountId = vendorTemplate.DefaultSalesTaxInputAccountId;
                }

                // Clear existing OCR-extracted line items before adding new ones
                var ocrLineItems = invoice.LineItems.Where(li => li.IsOcrExtracted).ToList();
                if (ocrLineItems.Any())
                {
                    _context.InvoiceLineItems.RemoveRange(ocrLineItems);
                }

                // Add OCR extracted line items
                var newLineItems = new List<InvoiceLineItem>();
                int lineNumber = invoice.LineItems.Count(li => !li.IsOcrExtracted) + 1;
                foreach (var ocrLineItem in ocrResult.ExtractedData.LineItems)
                {
                    var taxRate = defaultTaxRate;
                    var amount = ocrLineItem.Amount;
                    var taxAmount = amount * (taxRate / 100);

                    var lineItem = new InvoiceLineItem
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = invoice.Id,
                        LineNumber = lineNumber++,
                        Description = ocrLineItem.Description,
                        Quantity = ocrLineItem.Quantity,
                        UnitPrice = ocrLineItem.UnitPrice,
                        Amount = amount,
                        TaxRate = taxRate,
                        TaxAmount = taxAmount,
                        TotalAmount = amount + taxAmount,
                        ChartOfAccountId = defaultChartOfAccountId,
                        AccountCode = defaultAccountCode,
                        IsOcrExtracted = true,
                        OcrConfidenceScore = ocrLineItem.ConfidenceScore,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    // Explicitly add to DbSet to ensure EF tracks as Added (INSERT),
                    // not Modified (UPDATE) which causes DbUpdateConcurrencyException
                    _context.InvoiceLineItems.Add(lineItem);
                    newLineItems.Add(lineItem);
                }

                // Recalculate totals from actual line items (never trust OCR summary values
                // as they may include tax, be misread, or refer to different totals in the PDF)
                var allLineItems = invoice.LineItems.Where(li => !li.IsOcrExtracted).Concat(newLineItems).ToList();
                invoice.SubTotal = allLineItems.Sum(li => li.Amount);
                invoice.TaxAmount = allLineItems.Sum(li => li.TaxAmount);
                invoice.TotalAmount = invoice.SubTotal + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;

                await _context.SaveChangesAsync();

                var message = ocrResult.Success
                    ? $"OCR processing completed successfully. Confidence: {ocrResult.ConfidenceScore:F2}%"
                    : $"OCR extracted partial data (Confidence: {ocrResult.ConfidenceScore:F2}%). {ocrResult.ErrorMessage}";

                return (true, message);
            }
            else
            {
                invoice.OcrErrorMessage = ocrResult.ErrorMessage;
                invoice.OcrConfidenceScore = 0;
                await _context.SaveChangesAsync();

                return (false, ocrResult.ErrorMessage ?? "OCR processing failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing invoice with OCR");

            // Always persist the error state so it's visible in the UI
            try
            {
                // Clear stale/failed entities from change tracker to avoid
                // re-triggering the same DbUpdateConcurrencyException
                foreach (var entry in _context.ChangeTracker.Entries().ToList())
                {
                    entry.State = EntityState.Detached;
                }

                if (invoice != null)
                {
                    // Re-attach and update only the invoice with error state
                    var freshInvoice = await _context.Invoices.FindAsync(invoice.Id);
                    if (freshInvoice != null)
                    {
                        freshInvoice.IsOcrProcessed = true;
                        freshInvoice.OcrProcessedAt = DateTime.UtcNow;
                        freshInvoice.OcrErrorMessage = $"OCR processing failed: {ex.Message}";
                        freshInvoice.OcrConfidenceScore = 0;
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save OCR error state");
            }

            return (false, $"Error processing OCR: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> PostToGLAsync(Guid invoiceId, Guid userId)
    {
        try
        {
            if (!await CanUserAccessInvoiceAsync(invoiceId, userId))
            {
                return (false, "Access denied");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Vendor)
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                return (false, "Invoice not found");
            }

            if (invoice.IsPostedToGL)
            {
                return (false, "Invoice has already been posted to General Ledger");
            }

            if (invoice.Status != InvoiceStatus.Approved && invoice.Status != InvoiceStatus.Paid)
            {
                return (false, "Invoice must be Approved or Paid before posting to General Ledger");
            }

            // Load vendor template to check which accounts are required
            VendorInvoiceTemplate? vendorTemplate = null;
            if (invoice.VendorId.HasValue)
            {
                vendorTemplate = await _context.VendorInvoiceTemplates
                    .FirstOrDefaultAsync(t => t.VendorId == invoice.VendorId.Value && t.IsActive);
            }

            // Validate: All line items must have accounts assigned
            if (invoice.LineItems.Any())
            {
                var unassignedItems = invoice.LineItems.Where(li => !li.ChartOfAccountId.HasValue).ToList();
                if (unassignedItems.Any())
                    return (false, $"All line items must have an account assigned before posting to GL. {unassignedItems.Count} line item(s) are missing an account.");
            }

            // Validate: Line items sum must equal SubTotal
            if (invoice.LineItems.Any())
            {
                var lineItemsSum = invoice.LineItems.Sum(li => li.Amount);
                if (lineItemsSum != invoice.SubTotal)
                    return (false, $"Line items total ({lineItemsSum:N2}) does not match SubTotal ({invoice.SubTotal:N2}). Please correct the amounts before posting.");
            }

            // Validate: Advance Tax account required if enabled and amount > 0
            if (vendorTemplate == null || vendorTemplate.HasAdvanceTaxAccount)
            {
                if (invoice.AdvanceTaxAmount > 0 && !invoice.AdvanceTaxAccountId.HasValue)
                    return (false, "Advance Tax account must be assigned before posting to GL");
            }

            // Validate: Sales Tax Input account required if enabled and amount > 0
            if (vendorTemplate == null || vendorTemplate.HasSalesTaxInputAccount)
            {
                if (invoice.SalesTaxInputAmount > 0 && !invoice.SalesTaxInputAccountId.HasValue)
                    return (false, "Sales Tax Input account must be assigned before posting to GL");
            }

            // Validate: Payable Vendors account must be set in vendor template
            if (vendorTemplate == null || !vendorTemplate.DefaultPayableVendorsAccountId.HasValue)
                return (false, "Payable Vendors account must be configured in the vendor template before posting to GL");

            // Validate: Total Debits must equal Total Credits (accounting equation)
            var totalDebits = invoice.LineItems.Sum(li => li.Amount) + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;
            var totalCredits = invoice.TotalAmount;
            if (totalDebits != totalCredits)
                return (false, $"Accounting equation not balanced. Total Debits ({totalDebits:N2}) ≠ Total Credits ({totalCredits:N2}). Please verify the amounts.");

            invoice.IsPostedToGL = true;
            invoice.PostedToGLAt = DateTime.UtcNow;
            invoice.PostedToGLBy = userId;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} posted to GL by user {UserId}", invoiceId, userId);
            return (true, "Invoice successfully posted to General Ledger");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting invoice to GL");
            return (false, $"Error posting to General Ledger: {ex.Message}");
        }
    }

    public async Task<bool> CanUserAccessInvoiceAsync(Guid invoiceId, Guid userId)
    {
        var invoice = await _context.Invoices.FindAsync(invoiceId);
        if (invoice == null)
        {
            return false;
        }

        return await _context.UserCompanies
            .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == invoice.CompanyId);
    }
}
