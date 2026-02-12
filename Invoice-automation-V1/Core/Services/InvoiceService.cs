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

            // Check if invoice number already exists for this company
            var exists = await _context.Invoices
                .AnyAsync(i => i.CompanyId == model.CompanyId && i.InvoiceNumber == model.InvoiceNumber);

            if (exists)
            {
                return (false, $"Invoice number {model.InvoiceNumber} already exists for this company", null);
            }

            // Create invoice
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                CompanyId = model.CompanyId,
                VendorId = model.VendorId,
                InvoiceNumber = model.InvoiceNumber,
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
            invoice.TotalAmount = invoice.LineItems.Sum(li => li.TotalAmount);

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

            // Don't allow editing if invoice is paid or synced
            if (invoice.Status == InvoiceStatus.Paid || invoice.SyncedToIndraajAt.HasValue)
            {
                return (false, "Cannot edit a paid or synced invoice");
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
            invoice.TotalAmount = invoice.LineItems.Sum(li => li.TotalAmount);

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

            // Don't allow deleting paid or synced invoices
            if (invoice.Status == InvoiceStatus.Paid || invoice.SyncedToIndraajAt.HasValue)
            {
                return (false, "Cannot delete a paid or synced invoice");
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
            ApprovedByName = approvedByUser?.FullName,
            ApprovedAt = invoice.ApprovedAt,
            ApprovalNotes = invoice.ApprovalNotes,
            PaidAt = invoice.PaidAt,
            PaymentReference = invoice.PaymentReference,
            PaidByName = paidByUser?.FullName,
            IndraajVoucherNo = invoice.IndraajVoucherNo,
            SyncedToIndraajAt = invoice.SyncedToIndraajAt,
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

    public async Task<(bool Success, string Message)> SyncToIndraajAsync(Guid invoiceId, Guid userId)
    {
        try
        {
            var invoice = await _context.Invoices
                .Include(i => i.Company)
                .Include(i => i.Vendor)
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                return (false, "Invoice not found");
            }

            if (!await CanUserAccessInvoiceAsync(invoiceId, userId))
            {
                return (false, "You don't have permission to sync this invoice");
            }

            if (invoice.Status != InvoiceStatus.Approved && invoice.Status != InvoiceStatus.Paid)
            {
                return (false, "Only approved or paid invoices can be synced to Indraaj");
            }

            if (string.IsNullOrWhiteSpace(invoice.Company?.IndraajAccessToken))
            {
                return (false, "Company is not connected to Indraaj");
            }

            // TODO: Implement actual Indraaj API integration
            // For now, simulate the sync
            await Task.Delay(500);

            invoice.IndraajVoucherNo = $"VCH-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";
            invoice.SyncedToIndraajAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Invoice {InvoiceId} synced to Indraaj with voucher {VoucherNo}", invoiceId, invoice.IndraajVoucherNo);

            return (true, $"Invoice synced to Indraaj successfully. Voucher No: {invoice.IndraajVoucherNo}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing invoice to Indraaj");
            return (false, $"Error syncing to Indraaj: {ex.Message}");
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

                // Try to match vendor by name from OCR text
                if (!string.IsNullOrWhiteSpace(ocrResult.ExtractedData.VendorName))
                {
                    var vendorName = ocrResult.ExtractedData.VendorName;
                    var matchedVendor = await _context.Vendors
                        .Where(v => v.CompanyId == invoice.CompanyId && v.IsActive)
                        .ToListAsync();

                    // Do matching in memory for case-insensitive partial matching
                    var matched = matchedVendor.FirstOrDefault(v =>
                        v.Name.Contains(vendorName, StringComparison.OrdinalIgnoreCase) ||
                        vendorName.Contains(v.Name, StringComparison.OrdinalIgnoreCase));

                    if (matched != null)
                    {
                        invoice.VendorId = matched.Id;
                        _logger.LogInformation("Matched vendor '{VendorName}' (Id: {VendorId}) from OCR text",
                            matched.Name, matched.Id);
                    }
                }

                // Clear existing OCR-extracted line items before adding new ones
                var ocrLineItems = invoice.LineItems.Where(li => li.IsOcrExtracted).ToList();
                if (ocrLineItems.Any())
                {
                    _context.InvoiceLineItems.RemoveRange(ocrLineItems);
                }

                // Add OCR extracted line items
                int lineNumber = invoice.LineItems.Count(li => !li.IsOcrExtracted) + 1;
                foreach (var ocrLineItem in ocrResult.ExtractedData.LineItems)
                {
                    var lineItem = new InvoiceLineItem
                    {
                        Id = Guid.NewGuid(),
                        InvoiceId = invoice.Id,
                        LineNumber = lineNumber++,
                        Description = ocrLineItem.Description,
                        Quantity = ocrLineItem.Quantity,
                        UnitPrice = ocrLineItem.UnitPrice,
                        Amount = ocrLineItem.Amount,
                        TaxRate = 0,
                        TaxAmount = 0,
                        TotalAmount = ocrLineItem.Amount,
                        IsOcrExtracted = true,
                        OcrConfidenceScore = ocrLineItem.ConfidenceScore,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    invoice.LineItems.Add(lineItem);
                }

                // Recalculate totals from OCR data or line items
                invoice.SubTotal = ocrResult.ExtractedData.SubTotal ?? invoice.LineItems.Sum(li => li.Amount);
                invoice.TaxAmount = ocrResult.ExtractedData.TaxAmount ?? invoice.LineItems.Sum(li => li.TaxAmount);
                invoice.TotalAmount = ocrResult.ExtractedData.TotalAmount ?? invoice.LineItems.Sum(li => li.TotalAmount);

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
                if (invoice != null)
                {
                    invoice.IsOcrProcessed = true;
                    invoice.OcrProcessedAt = DateTime.UtcNow;
                    invoice.OcrErrorMessage = $"OCR processing failed: {ex.Message}";
                    invoice.OcrConfidenceScore = 0;
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save OCR error state");
            }

            return (false, $"Error processing OCR: {ex.Message}");
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
