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

        // First check if the user has a default company set
        var userCompany = await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId)
            .OrderByDescending(uc => uc.IsUserDefault)
            .ThenByDescending(uc => uc.Company!.IsDefault)
            .ThenBy(uc => uc.CreatedAt)
            .FirstOrDefaultAsync();

        return userCompany?.CompanyId ?? Guid.Empty;
    }

    // GET: Invoice/SelectCompany
    public async Task<IActionResult> SelectCompany(string? returnAction)
    {
        var userId = GetCurrentUserId();
        var companies = await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId && uc.Company!.IsActive)
            .OrderByDescending(uc => uc.IsUserDefault)
            .ThenBy(uc => uc.Company!.Name)
            .Select(uc => new InvoiceAutomation.Core.DTOs.Company.CompanyDropdownDto
            {
                Id = uc.CompanyId,
                Name = uc.Company!.Name,
                Ntn = uc.Company.Ntn,
                IsUserDefault = uc.IsUserDefault,
                Role = uc.Role.ToString()
            })
            .ToListAsync();

        if (companies == null || !companies.Any())
        {
            TempData["Error"] = "Please set up a company first.";
            return RedirectToAction("Index", "Company");
        }

        // Determine which action to redirect to
        var targetAction = string.Equals(returnAction, "Create", StringComparison.OrdinalIgnoreCase)
            ? nameof(Create)
            : nameof(Index);

        // If user has only one company, skip selection and go directly
        if (companies.Count == 1)
        {
            return RedirectToAction(targetAction, new { companyId = companies[0].Id });
        }

        ViewData["Title"] = "Select Company - Invoices";
        ViewData["TargetAction"] = targetAction;
        ViewData["TargetController"] = "Invoice";
        ViewData["SectionTitle"] = "Invoices";
        ViewData["SectionDescription"] = targetAction == nameof(Create)
            ? "Select a company to upload an invoice for"
            : "Select a company to manage its invoices";
        ViewData["SectionIcon"] = "bi-receipt";

        return View(companies);
    }

    // GET: Invoice
    public async Task<IActionResult> Index(Guid? companyId, string? status, string? search, int page = 1)
    {
        Guid resolvedCompanyId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            resolvedCompanyId = companyId.Value;
        }
        else
        {
            return RedirectToAction(nameof(SelectCompany));
        }

        const int pageSize = 20;
        var viewModel = await _invoiceService.GetInvoicesAsync(resolvedCompanyId, page, pageSize, status, search);

        ViewBag.CompanyId = resolvedCompanyId;
        return View(viewModel);
    }

    // GET: Invoice/Create?companyId=xxx
    public async Task<IActionResult> Create(Guid? companyId)
    {
        Guid resolvedCompanyId;
        if (companyId.HasValue && companyId.Value != Guid.Empty)
        {
            resolvedCompanyId = companyId.Value;
        }
        else
        {
            return RedirectToAction(nameof(SelectCompany), new { returnAction = "Create" });
        }

        await PopulateDropdownsAsync(resolvedCompanyId);

        var model = new CreateInvoiceViewModel
        {
            CompanyId = resolvedCompanyId,
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

        // Populate chart of accounts for inline assignment
        await PopulateDropdownsAsync(viewModel.CompanyId);

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

    // POST: Invoice/PostToGL/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PostToGL(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _invoiceService.PostToGLAsync(id, userId);

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
            _logger.LogError(ex, "Error posting invoice to GL");
            TempData["Error"] = "An error occurred while posting to General Ledger";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: Invoice/GetGLPreview?id=xxx (AJAX - returns journal entries that will be posted)
    [HttpGet]
    public async Task<IActionResult> GetGLPreview(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!await _invoiceService.CanUserAccessInvoiceAsync(id, userId))
                return Json(new { success = false, message = "Access denied" });

            var invoice = await _context.Invoices
                .Include(i => i.Vendor)
                .Include(i => i.LineItems)
                    .ThenInclude(li => li.ChartOfAccount)
                .Include(i => i.AdvanceTaxAccount)
                .Include(i => i.SalesTaxInputAccount)
                .Include(i => i.PayableVendorsAccount)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null)
                return Json(new { success = false, message = "Invoice not found" });

            if (invoice.IsPostedToGL)
                return Json(new { success = false, message = "Invoice has already been posted to GL" });

            if (invoice.Status != InvoiceStatus.Approved && invoice.Status != InvoiceStatus.Paid)
                return Json(new { success = false, message = "Invoice must be Approved or Paid before posting to GL" });

            // Build journal entries preview
            var entries = new List<GLPreviewEntry>();

            // Debit: Line item accounts (grouped by account)
            var lineItemsByAccount = invoice.LineItems
                .Where(li => li.ChartOfAccountId.HasValue && li.ChartOfAccount != null)
                .GroupBy(li => new { li.ChartOfAccountId, li.ChartOfAccount!.Code, li.ChartOfAccount.Name })
                .ToList();

            foreach (var group in lineItemsByAccount)
            {
                var amount = group.Sum(li => li.Amount);
                if (amount > 0)
                {
                    entries.Add(new GLPreviewEntry
                    {
                        AccountCode = group.Key.Code,
                        AccountName = group.Key.Name,
                        Debit = amount,
                        Credit = 0m
                    });
                }
            }

            // Debit: Advance Tax Account (Advance Tax Amount)
            if (invoice.AdvanceTaxAccountId.HasValue && invoice.AdvanceTaxAccount != null && invoice.AdvanceTaxAmount > 0)
            {
                entries.Add(new GLPreviewEntry
                {
                    AccountCode = invoice.AdvanceTaxAccount.Code,
                    AccountName = invoice.AdvanceTaxAccount.Name,
                    Debit = invoice.AdvanceTaxAmount,
                    Credit = 0m
                });
            }

            // Debit: Sales Tax Input Account (Sales Tax Input Amount)
            if (invoice.SalesTaxInputAccountId.HasValue && invoice.SalesTaxInputAccount != null && invoice.SalesTaxInputAmount > 0)
            {
                entries.Add(new GLPreviewEntry
                {
                    AccountCode = invoice.SalesTaxInputAccount.Code,
                    AccountName = invoice.SalesTaxInputAccount.Name,
                    Debit = invoice.SalesTaxInputAmount,
                    Credit = 0m
                });
            }

            // Credit: Payable Vendors Account (Total Amount)
            if (invoice.PayableVendorsAccountId.HasValue && invoice.PayableVendorsAccount != null && invoice.TotalAmount > 0)
            {
                entries.Add(new GLPreviewEntry
                {
                    AccountCode = invoice.PayableVendorsAccount.Code,
                    AccountName = invoice.PayableVendorsAccount.Name,
                    Debit = 0m,
                    Credit = invoice.TotalAmount
                });
            }

            if (!entries.Any())
                return Json(new { success = false, message = "No GL accounts are assigned. Please assign line item accounts before posting." });

            return Json(new { success = true, entries });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating GL preview");
            return Json(new { success = false, message = "An error occurred generating the GL preview" });
        }
    }

    // POST: Invoice/UpdateInvoiceAccount (AJAX - for inline GL account assignment)
    [HttpPost]
    public async Task<IActionResult> UpdateInvoiceAccount([FromBody] UpdateInvoiceAccountRequest request)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(request.InvoiceId);
            if (invoice == null)
                return Json(new { success = false, message = "Invoice not found" });

            var userId = GetCurrentUserId();
            if (!await _invoiceService.CanUserAccessInvoiceAsync(request.InvoiceId, userId))
                return Json(new { success = false, message = "Access denied" });

            if (invoice.IsPostedToGL)
                return Json(new { success = false, message = "Cannot change accounts after posting to GL" });

            Guid? accountId = null;
            string? accountName = null;
            if (Guid.TryParse(request.AccountId, out var parsedId) && parsedId != Guid.Empty)
            {
                var account = await _context.ChartOfAccounts.FindAsync(parsedId);
                if (account == null)
                    return Json(new { success = false, message = "Account not found" });
                accountId = account.Id;
                accountName = account.DisplayName;
            }

            switch (request.AccountType.ToLower())
            {
                case "advancetax":
                    invoice.AdvanceTaxAccountId = accountId;
                    break;
                case "salestaxinput":
                    invoice.SalesTaxInputAccountId = accountId;
                    break;
                case "payablevendors":
                    invoice.PayableVendorsAccountId = accountId;
                    break;
                default:
                    return Json(new { success = false, message = "Unknown account type" });
            }

            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true, accountName = accountName ?? "" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice account");
            return Json(new { success = false, message = "An error occurred" });
        }
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
        var userId = GetCurrentUserId();

        // Populate companies the user has access to
        var companies = await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId && uc.Company!.IsActive)
            .OrderByDescending(uc => uc.Company!.IsDefault)
            .ThenBy(uc => uc.Company!.DisplayOrder)
            .ThenBy(uc => uc.Company!.Name)
            .Select(uc => new SelectListItem
            {
                Value = uc.CompanyId.ToString(),
                Text = uc.Company!.Name,
                Selected = uc.CompanyId == companyId
            })
            .ToListAsync();

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

        ViewBag.Companies = companies;
        ViewBag.Vendors = vendors;
        ViewBag.ChartOfAccounts = accounts;
    }

    // POST: Invoice/AddLineItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddLineItem(Guid invoiceId, string description, decimal quantity, decimal unitPrice, decimal taxRate, Guid? chartOfAccountId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!await _invoiceService.CanUserAccessInvoiceAsync(invoiceId, userId))
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction(nameof(Index));
            }

            var invoice = await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);

            if (invoice == null)
            {
                TempData["Error"] = "Invoice not found";
                return RedirectToAction(nameof(Index));
            }

            var amount = quantity * unitPrice;
            var taxAmount = amount * (taxRate / 100);
            var totalAmount = amount + taxAmount;

            string? accountCode = null;
            if (chartOfAccountId.HasValue && chartOfAccountId.Value != Guid.Empty)
            {
                var account = await _context.ChartOfAccounts.FindAsync(chartOfAccountId.Value);
                accountCode = account?.Code;
            }

            var lineItem = new InvoiceLineItem
            {
                Id = Guid.NewGuid(),
                InvoiceId = invoiceId,
                LineNumber = (invoice.LineItems.Any() ? invoice.LineItems.Max(li => li.LineNumber) : 0) + 1,
                Description = description,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Amount = amount,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                TotalAmount = totalAmount,
                ChartOfAccountId = (chartOfAccountId.HasValue && chartOfAccountId.Value != Guid.Empty) ? chartOfAccountId : null,
                AccountCode = accountCode,
                IsOcrExtracted = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.InvoiceLineItems.Add(lineItem);
            invoice.LineItems.Add(lineItem);

            // Recalculate invoice totals
            invoice.SubTotal = invoice.LineItems.Sum(li => li.Amount);
            invoice.TaxAmount = invoice.LineItems.Sum(li => li.TaxAmount);
            invoice.TotalAmount = invoice.SubTotal + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Line item added successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding line item");
            TempData["Error"] = "An error occurred while adding the line item";
        }

        return RedirectToAction(nameof(Details), new { id = invoiceId });
    }

    // POST: Invoice/UpdateLineItem (AJAX)
    [HttpPost]
    public async Task<IActionResult> UpdateLineItem([FromBody] UpdateLineItemRequest request)
    {
        try
        {
            var lineItem = await _context.InvoiceLineItems
                .Include(li => li.Invoice)
                .ThenInclude(i => i!.LineItems)
                .FirstOrDefaultAsync(li => li.Id == request.LineItemId);

            if (lineItem == null)
                return Json(new { success = false, message = "Line item not found" });

            var userId = GetCurrentUserId();
            if (!await _invoiceService.CanUserAccessInvoiceAsync(lineItem.InvoiceId, userId))
                return Json(new { success = false, message = "Access denied" });

            // Check invoice is editable
            var invoice = lineItem.Invoice!;
            if (invoice.Status == InvoiceStatus.Paid)
                return Json(new { success = false, message = "Invoice cannot be edited" });

            lineItem.Description = request.Description;
            lineItem.Quantity = request.Quantity;
            lineItem.UnitPrice = request.UnitPrice;
            lineItem.TaxRate = request.TaxRate;
            lineItem.Amount = request.Quantity * request.UnitPrice;
            lineItem.TaxAmount = lineItem.Amount * (request.TaxRate / 100);
            lineItem.TotalAmount = lineItem.Amount + lineItem.TaxAmount;
            lineItem.UpdatedAt = DateTime.UtcNow;

            if (request.ChartOfAccountId.HasValue && request.ChartOfAccountId.Value != Guid.Empty)
            {
                var account = await _context.ChartOfAccounts.FindAsync(request.ChartOfAccountId.Value);
                lineItem.ChartOfAccountId = account?.Id;
                lineItem.AccountCode = account?.Code;
            }
            else
            {
                lineItem.ChartOfAccountId = null;
                lineItem.AccountCode = null;
            }

            // Recalculate invoice totals
            invoice.SubTotal = invoice.LineItems.Sum(li => li.Amount);
            invoice.TaxAmount = invoice.LineItems.Sum(li => li.TaxAmount);
            invoice.TotalAmount = invoice.SubTotal + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                lineItem = new
                {
                    amount = lineItem.Amount,
                    taxAmount = lineItem.TaxAmount,
                    totalAmount = lineItem.TotalAmount
                },
                invoice = new
                {
                    subTotal = invoice.SubTotal,
                    taxAmount = invoice.TaxAmount,
                    totalAmount = invoice.TotalAmount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating line item");
            return Json(new { success = false, message = "An error occurred" });
        }
    }

    // POST: Invoice/UpdateInvoiceField (AJAX - for inline header editing)
    [HttpPost]
    public async Task<IActionResult> UpdateInvoiceField([FromBody] UpdateInvoiceFieldRequest request)
    {
        try
        {
            var invoice = await _context.Invoices.FindAsync(request.InvoiceId);
            if (invoice == null)
                return Json(new { success = false, message = "Invoice not found" });

            var userId = GetCurrentUserId();
            if (!await _invoiceService.CanUserAccessInvoiceAsync(request.InvoiceId, userId))
                return Json(new { success = false, message = "Access denied" });

            if (invoice.Status == InvoiceStatus.Paid)
                return Json(new { success = false, message = "Invoice cannot be edited" });

            switch (request.Field.ToLower())
            {
                case "vendorid":
                    if (Guid.TryParse(request.Value, out var vendorId))
                    {
                        var vendor = await _context.Vendors.FindAsync(vendorId);
                        if (vendor != null && vendor.CompanyId == invoice.CompanyId)
                            invoice.VendorId = vendorId;
                        else
                            return Json(new { success = false, message = "Invalid vendor" });
                    }
                    break;
                case "invoicenumber":
                    if (!string.IsNullOrWhiteSpace(request.Value))
                        invoice.InvoiceNumber = request.Value;
                    break;
                case "invoicedate":
                    if (DateTime.TryParse(request.Value, out var invoiceDate))
                        invoice.InvoiceDate = invoiceDate;
                    break;
                case "duedate":
                    if (DateTime.TryParse(request.Value, out var dueDate))
                        invoice.DueDate = dueDate;
                    else if (string.IsNullOrWhiteSpace(request.Value))
                        invoice.DueDate = null;
                    break;
                case "description":
                    invoice.Description = request.Value;
                    break;
                case "notes":
                    invoice.Notes = request.Value;
                    break;
                case "advancetaxamount":
                    if (decimal.TryParse(request.Value, out var advanceTaxAmount) && advanceTaxAmount >= 0)
                    {
                        invoice.AdvanceTaxAmount = advanceTaxAmount;
                        // Recalculate total: SubTotal + AdvanceTaxAmount + SalesTaxInputAmount
                        invoice.TotalAmount = invoice.SubTotal + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;
                    }
                    break;
                case "salestaxinputamount":
                    if (decimal.TryParse(request.Value, out var salesTaxInputAmount) && salesTaxInputAmount >= 0)
                    {
                        invoice.SalesTaxInputAmount = salesTaxInputAmount;
                        // Recalculate total: SubTotal + AdvanceTaxAmount + SalesTaxInputAmount
                        invoice.TotalAmount = invoice.SubTotal + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;
                    }
                    break;
                default:
                    return Json(new { success = false, message = "Unknown field" });
            }

            invoice.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                totalAmount = invoice.TotalAmount,
                advanceTaxAmount = invoice.AdvanceTaxAmount,
                salesTaxInputAmount = invoice.SalesTaxInputAmount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice field");
            return Json(new { success = false, message = "An error occurred" });
        }
    }

    // POST: Invoice/DeleteLineItem
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteLineItem(Guid lineItemId)
    {
        try
        {
            var lineItem = await _context.InvoiceLineItems
                .Include(li => li.Invoice)
                .ThenInclude(i => i!.LineItems)
                .FirstOrDefaultAsync(li => li.Id == lineItemId);

            if (lineItem == null)
            {
                TempData["Error"] = "Line item not found";
                return RedirectToAction(nameof(Index));
            }

            var userId = GetCurrentUserId();
            if (!await _invoiceService.CanUserAccessInvoiceAsync(lineItem.InvoiceId, userId))
            {
                TempData["Error"] = "Access denied";
                return RedirectToAction(nameof(Index));
            }

            var invoice = lineItem.Invoice!;
            var invoiceId = lineItem.InvoiceId;

            _context.InvoiceLineItems.Remove(lineItem);

            // Recalculate invoice totals
            var remainingItems = invoice.LineItems.Where(li => li.Id != lineItemId).ToList();
            invoice.SubTotal = remainingItems.Sum(li => li.Amount);
            invoice.TaxAmount = remainingItems.Sum(li => li.TaxAmount);
            invoice.TotalAmount = invoice.SubTotal + invoice.AdvanceTaxAmount + invoice.SalesTaxInputAmount;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Line item deleted";
            return RedirectToAction(nameof(Details), new { id = invoiceId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting line item");
            TempData["Error"] = "An error occurred while deleting the line item";
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Invoice/AssignLineItemAccount
    [HttpPost]
    public async Task<IActionResult> AssignLineItemAccount([FromBody] AssignAccountRequest request)
    {
        try
        {
            var lineItem = await _context.InvoiceLineItems
                .Include(li => li.Invoice)
                .FirstOrDefaultAsync(li => li.Id == request.LineItemId);

            if (lineItem == null)
            {
                return Json(new { success = false, message = "Line item not found" });
            }

            var userId = GetCurrentUserId();
            if (!await _invoiceService.CanUserAccessInvoiceAsync(lineItem.InvoiceId, userId))
            {
                return Json(new { success = false, message = "Access denied" });
            }

            if (request.ChartOfAccountId.HasValue && request.ChartOfAccountId.Value != Guid.Empty)
            {
                var account = await _context.ChartOfAccounts.FindAsync(request.ChartOfAccountId.Value);
                if (account == null)
                {
                    return Json(new { success = false, message = "Account not found" });
                }

                lineItem.ChartOfAccountId = account.Id;
                lineItem.AccountCode = account.Code;
            }
            else
            {
                lineItem.ChartOfAccountId = null;
                lineItem.AccountCode = null;
            }

            lineItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning account to line item");
            return Json(new { success = false, message = "An error occurred" });
        }
    }

    // API endpoint: returns vendors for a given company (used by AJAX on company change)
    [HttpGet]
    public async Task<IActionResult> GetVendorsByCompany(Guid companyId)
    {
        var userId = GetCurrentUserId();

        // Verify user has access to the company
        var hasAccess = await _context.UserCompanies
            .AnyAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);

        if (!hasAccess)
        {
            return Forbid();
        }

        var vendors = await _context.Vendors
            .Where(v => v.CompanyId == companyId && v.IsActive)
            .OrderBy(v => v.Name)
            .Select(v => new { v.Id, v.Name })
            .ToListAsync();

        return Json(vendors);
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

    public class AssignAccountRequest
    {
        public Guid LineItemId { get; set; }
        public Guid? ChartOfAccountId { get; set; }
    }

    public class UpdateLineItemRequest
    {
        public Guid LineItemId { get; set; }
        public string Description { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public Guid? ChartOfAccountId { get; set; }
    }

    public class UpdateInvoiceFieldRequest
    {
        public Guid InvoiceId { get; set; }
        public string Field { get; set; } = string.Empty;
        public string? Value { get; set; }
    }

    public class UpdateInvoiceAccountRequest
    {
        public Guid InvoiceId { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public string? AccountId { get; set; }
    }

    public class GLPreviewEntry
    {
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
