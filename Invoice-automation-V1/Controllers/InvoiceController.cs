using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.Infrastructure.Data;
using Invoice_automation_V1.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Invoice_automation_V1.Controllers;

[Authorize]
public class InvoiceController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IInvoiceService _invoiceService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<InvoiceController> _logger;

    public InvoiceController(
        ApplicationDbContext context,
        IInvoiceService invoiceService,
        IWebHostEnvironment environment,
        ILogger<InvoiceController> logger)
    {
        _context = context;
        _invoiceService = invoiceService;
        _environment = environment;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private async Task<Guid> GetDefaultCompanyIdAsync()
    {
        var userId = GetCurrentUserId();
        var userCompany = await _context.UserCompanies
            .Where(uc => uc.UserId == userId)
            .OrderByDescending(uc => uc.IsUserDefault)
            .ThenBy(uc => uc.CreatedAt)
            .FirstOrDefaultAsync();

        return userCompany?.CompanyId ?? Guid.Empty;
    }

    // GET: Invoice
    public async Task<IActionResult> Index(string? status, string? search, int page = 1)
    {
        var companyId = await GetDefaultCompanyIdAsync();
        if (companyId == Guid.Empty)
        {
            TempData["Error"] = "Please set up a company first.";
            return RedirectToAction("Index", "Company");
        }

        const int pageSize = 20;
        var viewModel = await _invoiceService.GetInvoicesAsync(companyId, page, pageSize, status, search);

        ViewBag.CompanyId = companyId;
        return View(viewModel);
    }

    // GET: Invoice/Create
    public async Task<IActionResult> Create()
    {
        var companyId = await GetDefaultCompanyIdAsync();
        if (companyId == Guid.Empty)
        {
            TempData["Error"] = "Please set up a company first.";
            return RedirectToAction("Index", "Company");
        }

        await PopulateDropdownsAsync(companyId);

        var model = new CreateInvoiceViewModel
        {
            CompanyId = companyId,
            InvoiceDate = DateTime.Today,
            DueDate = DateTime.Today.AddDays(30),
            ProcessWithOcr = true
        };

        return View(model);
    }

    // POST: Invoice/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateInvoiceViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CompanyId);
            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();

            // Handle file upload
            if (model.InvoiceFile != null && model.InvoiceFile.Length > 0)
            {
                var uploadResult = await SaveUploadedFileAsync(model.InvoiceFile);
                if (!uploadResult.Success)
                {
                    ModelState.AddModelError("InvoiceFile", uploadResult.ErrorMessage ?? "File upload failed");
                    await PopulateDropdownsAsync(model.CompanyId);
                    return View(model);
                }

                // Create the invoice
                var result = await _invoiceService.CreateInvoiceAsync(model, userId);

                if (result.Success && result.InvoiceId.HasValue)
                {
                    // Update the invoice with file information
                    var invoice = await _context.Invoices.FindAsync(result.InvoiceId.Value);
                    if (invoice != null)
                    {
                        invoice.OriginalFileName = uploadResult.OriginalFileName;
                        invoice.FileStoragePath = uploadResult.FilePath;
                        invoice.FileUrl = uploadResult.FileUrl;
                        invoice.FileType = uploadResult.FileType;
                        invoice.FileSize = uploadResult.FileSize;
                        await _context.SaveChangesAsync();

                        // Process with OCR if requested
                        if (model.ProcessWithOcr)
                        {
                            var ocrResult = await _invoiceService.ProcessOcrAsync(result.InvoiceId.Value);
                            if (ocrResult.Success)
                            {
                                TempData["Success"] = $"Invoice created successfully! {ocrResult.Message}";
                            }
                            else
                            {
                                TempData["Warning"] = $"Invoice created but OCR processing failed: {ocrResult.Message}";
                            }
                        }
                        else
                        {
                            TempData["Success"] = "Invoice created successfully!";
                        }
                    }

                    return RedirectToAction(nameof(Details), new { id = result.InvoiceId.Value });
                }
                else
                {
                    ModelState.AddModelError("", result.Message);
                }
            }
            else
            {
                ModelState.AddModelError("InvoiceFile", "Please upload an invoice file");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            ModelState.AddModelError("", "An error occurred while creating the invoice");
        }

        await PopulateDropdownsAsync(model.CompanyId);
        return View(model);
    }

    // GET: Invoice/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = GetCurrentUserId();
        var viewModel = await _invoiceService.GetInvoiceDetailsAsync(id, userId);

        if (viewModel == null)
        {
            TempData["Error"] = "Invoice not found or you don't have permission to view it.";
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Invoice/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = GetCurrentUserId();
        var invoice = await _invoiceService.GetInvoiceDetailsAsync(id, userId);

        if (invoice == null)
        {
            TempData["Error"] = "Invoice not found or you don't have permission to edit it.";
            return RedirectToAction(nameof(Index));
        }

        var model = new EditInvoiceViewModel
        {
            Id = invoice.Id,
            CompanyId = invoice.CompanyId,
            VendorId = invoice.VendorId,
            InvoiceNumber = invoice.InvoiceNumber,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Description = invoice.Description,
            Notes = invoice.Notes,
            Status = invoice.Status,
            LineItems = invoice.LineItems,
            OriginalFileName = invoice.OriginalFileName,
            FileUrl = invoice.FileUrl
        };

        await PopulateDropdownsAsync(invoice.CompanyId);
        return View(model);
    }

    // POST: Invoice/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, EditInvoiceViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model.CompanyId);
            return View(model);
        }

        try
        {
            var userId = GetCurrentUserId();

            // Handle new file upload if provided
            if (model.NewInvoiceFile != null && model.NewInvoiceFile.Length > 0)
            {
                var uploadResult = await SaveUploadedFileAsync(model.NewInvoiceFile);
                if (uploadResult.Success)
                {
                    var invoice = await _context.Invoices.FindAsync(model.Id);
                    if (invoice != null)
                    {
                        // Delete old file if exists
                        if (!string.IsNullOrWhiteSpace(invoice.FileStoragePath))
                        {
                            DeleteFile(invoice.FileStoragePath);
                        }

                        invoice.OriginalFileName = uploadResult.OriginalFileName;
                        invoice.FileStoragePath = uploadResult.FilePath;
                        invoice.FileUrl = uploadResult.FileUrl;
                        invoice.FileType = uploadResult.FileType;
                        invoice.FileSize = uploadResult.FileSize;
                        await _context.SaveChangesAsync();
                    }
                }
            }

            var result = await _invoiceService.UpdateInvoiceAsync(model, userId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
                return RedirectToAction(nameof(Details), new { id = model.Id });
            }
            else
            {
                ModelState.AddModelError("", result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice");
            ModelState.AddModelError("", "An error occurred while updating the invoice");
        }

        await PopulateDropdownsAsync(model.CompanyId);
        return View(model);
    }

    // POST: Invoice/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _invoiceService.DeleteInvoiceAsync(id, userId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting invoice");
            TempData["Error"] = "An error occurred while deleting the invoice";
        }

        return RedirectToAction(nameof(Index));
    }

    // POST: Invoice/Approve/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, string? approvalNotes)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _invoiceService.ApproveInvoiceAsync(id, userId, approvalNotes);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving invoice");
            TempData["Error"] = "An error occurred while approving the invoice";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Invoice/Reject/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string? rejectionNotes)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _invoiceService.RejectInvoiceAsync(id, userId, rejectionNotes);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting invoice");
            TempData["Error"] = "An error occurred while rejecting the invoice";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Invoice/MarkAsPaid/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsPaid(Guid id, DateTime paymentDate, string paymentReference)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _invoiceService.MarkAsPaidAsync(id, userId, paymentDate, paymentReference);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking invoice as paid");
            TempData["Error"] = "An error occurred while marking the invoice as paid";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Invoice/SyncToIndraaj/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncToIndraaj(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _invoiceService.SyncToIndraajAsync(id, userId);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing invoice to Indraaj");
            TempData["Error"] = "An error occurred while syncing to Indraaj";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: Invoice/ProcessOcr/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessOcr(Guid id)
    {
        try
        {
            var result = await _invoiceService.ProcessOcrAsync(id);

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OCR");
            TempData["Error"] = "An error occurred while processing OCR";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task PopulateDropdownsAsync(Guid companyId)
    {
        var vendors = await _context.Vendors
            .Where(v => v.CompanyId == companyId && v.IsActive)
            .OrderBy(v => v.Name)
            .Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = v.Name
            })
            .ToListAsync();

        var accounts = await _context.ChartOfAccounts
            .Where(a => a.CompanyId == companyId && a.IsActive)
            .OrderBy(a => a.Code)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.Name}"
            })
            .ToListAsync();

        ViewBag.Vendors = vendors;
        ViewBag.ChartOfAccounts = accounts;
    }

    private async Task<FileUploadResult> SaveUploadedFileAsync(IFormFile file)
    {
        try
        {
            // Validate file
            var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".tif", ".tiff" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(extension))
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "Invalid file type. Allowed types: PDF, JPG, PNG, TIF"
                };
            }

            const long maxFileSize = 10 * 1024 * 1024; // 10 MB
            if (file.Length > maxFileSize)
            {
                return new FileUploadResult
                {
                    Success = false,
                    ErrorMessage = "File size exceeds 10 MB limit"
                };
            }

            // Create upload directory
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "invoices");
            Directory.CreateDirectory(uploadsFolder);

            // Generate unique filename
            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return new FileUploadResult
            {
                Success = true,
                FilePath = filePath,
                FileUrl = $"/uploads/invoices/{uniqueFileName}",
                OriginalFileName = file.FileName,
                FileType = extension.TrimStart('.'),
                FileSize = file.Length
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving uploaded file");
            return new FileUploadResult
            {
                Success = false,
                ErrorMessage = "Error saving file"
            };
        }
    }

    private void DeleteFile(string filePath)
    {
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {FilePath}", filePath);
        }
    }

    private class FileUploadResult
    {
        public bool Success { get; set; }
        public string? FilePath { get; set; }
        public string? FileUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public string? FileType { get; set; }
        public long? FileSize { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
