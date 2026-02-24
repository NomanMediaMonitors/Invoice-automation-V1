using InvoiceAutomation.Core.DTOs.Vendor;
using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InvoiceAutomation.Controllers;

[Authorize]
public class VendorController : Controller
{
    private readonly IVendorService _vendorService;
    private readonly ICompanyService _companyService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<VendorController> _logger;

    public VendorController(
        IVendorService vendorService,
        ICompanyService companyService,
        ApplicationDbContext context,
        ILogger<VendorController> logger)
    {
        _vendorService = vendorService;
        _companyService = companyService;
        _context = context;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    private async Task<Guid> GetDefaultCompanyIdAsync()
    {
        var userId = GetCurrentUserId();
        var userCompany = await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId)
            .OrderByDescending(uc => uc.IsUserDefault)
            .ThenByDescending(uc => uc.Company!.IsDefault)
            .ThenBy(uc => uc.CreatedAt)
            .FirstOrDefaultAsync();
        return userCompany?.CompanyId ?? Guid.Empty;
    }

    // GET: Vendor/SelectCompany
    public async Task<IActionResult> SelectCompany()
    {
        var userId = GetCurrentUserId();
        var companies = await _companyService.GetUserCompaniesAsync(userId);

        if (companies == null || !companies.Any())
        {
            TempData["ErrorMessage"] = "Please set up a company first.";
            return RedirectToAction("Index", "Company");
        }

        // If user has only one company, skip selection and go directly
        if (companies.Count == 1)
        {
            return RedirectToAction(nameof(Index), new { companyId = companies[0].Id });
        }

        ViewData["Title"] = "Select Company - Vendors";
        ViewData["TargetAction"] = "Index";
        ViewData["TargetController"] = "Vendor";
        ViewData["SectionTitle"] = "Vendors";
        ViewData["SectionDescription"] = "Select a company to manage its vendors";
        ViewData["SectionIcon"] = "bi-truck";

        return View(companies);
    }

    // GET: Vendor?companyId=xxx
    public async Task<IActionResult> Index(Guid? companyId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // If no companyId provided, redirect to company selection
            var resolvedCompanyId = companyId ?? Guid.Empty;
            if (resolvedCompanyId == Guid.Empty)
            {
                return RedirectToAction(nameof(SelectCompany));
            }

            // Check if user has access to this company
            var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, resolvedCompanyId);
            if (userCompany == null)
            {
                return NotFound();
            }

            var company = await _companyService.GetByIdAsync(resolvedCompanyId);
            if (company == null)
            {
                return NotFound();
            }

            var vendors = await _vendorService.GetCompanyVendorsAsync(resolvedCompanyId);

            ViewBag.CompanyId = resolvedCompanyId;
            ViewBag.CompanyName = company.Name;
            ViewBag.UserRole = userCompany.Role;

            return View(vendors);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vendors for company {CompanyId}", companyId);
            TempData["ErrorMessage"] = "An error occurred while loading vendors.";
            return RedirectToAction("Index", "Company");
        }
    }

    // GET: Vendor/Details/5?companyId=xxx
    public async Task<IActionResult> Details(Guid id, Guid companyId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            // Check if user has access to this company
            var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
            if (userCompany == null)
            {
                return NotFound();
            }

            var vendor = await _vendorService.GetVendorDetailsAsync(id);
            if (vendor == null || vendor.CompanyId != companyId)
            {
                return NotFound();
            }

            var company = await _companyService.GetByIdAsync(companyId);

            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            ViewBag.UserRole = userCompany.Role;

            return View(vendor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading vendor details");
            TempData["ErrorMessage"] = "An error occurred while loading vendor details.";
            return RedirectToAction(nameof(Index), new { companyId });
        }
    }

    // GET: Vendor/Create?companyId=xxx
    public async Task<IActionResult> Create(Guid companyId)
    {
        var currentUserId = GetCurrentUserId();

        var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
        if (userCompany == null)
        {
            return NotFound();
        }

        var company = await _companyService.GetByIdAsync(companyId);
        if (company == null)
        {
            return NotFound();
        }

        ViewBag.CompanyId = companyId;
        ViewBag.CompanyName = company.Name;

        return View(new CreateVendorDto { CompanyId = companyId });
    }

    // POST: Vendor/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateVendorDto dto)
    {
        if (!ModelState.IsValid)
        {
            var company = await _companyService.GetByIdAsync(dto.CompanyId);
            ViewBag.CompanyId = dto.CompanyId;
            ViewBag.CompanyName = company?.Name;
            return View(dto);
        }

        try
        {
            var currentUserId = GetCurrentUserId();

            var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, dto.CompanyId);
            if (userCompany == null)
            {
                return NotFound();
            }

            await _vendorService.CreateVendorAsync(dto, currentUserId);

            TempData["SuccessMessage"] = $"Vendor '{dto.Name}' created successfully!";
            return RedirectToAction(nameof(Index), new { companyId = dto.CompanyId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var company = await _companyService.GetByIdAsync(dto.CompanyId);
            ViewBag.CompanyId = dto.CompanyId;
            ViewBag.CompanyName = company?.Name;
            return View(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating vendor");
            ModelState.AddModelError("", "An error occurred while creating the vendor. Please try again.");
            var company = await _companyService.GetByIdAsync(dto.CompanyId);
            ViewBag.CompanyId = dto.CompanyId;
            ViewBag.CompanyName = company?.Name;
            return View(dto);
        }
    }

    // GET: Vendor/Edit/5?companyId=xxx
    public async Task<IActionResult> Edit(Guid id, Guid companyId)
    {
        var currentUserId = GetCurrentUserId();

        var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
        if (userCompany == null)
        {
            return NotFound();
        }

        var vendor = await _vendorService.GetVendorDetailsAsync(id);
        if (vendor == null || vendor.CompanyId != companyId)
        {
            return NotFound();
        }

        var company = await _companyService.GetByIdAsync(companyId);

        ViewBag.CompanyId = companyId;
        ViewBag.CompanyName = company?.Name;

        var dto = new UpdateVendorDto
        {
            Name = vendor.Name,
            Email = vendor.Email,
            Phone = vendor.Phone,
            MobilePhone = vendor.MobilePhone,
            Website = vendor.Website,
            Type = vendor.Type,
            Ntn = vendor.Ntn,
            Strn = vendor.Strn,
            RegistrationNumber = vendor.RegistrationNumber,
            Address = vendor.Address,
            City = vendor.City,
            State = vendor.State,
            PostalCode = vendor.PostalCode,
            Country = vendor.Country,
            ContactPersonName = vendor.ContactPersonName,
            ContactPersonEmail = vendor.ContactPersonEmail,
            ContactPersonPhone = vendor.ContactPersonPhone,
            BankName = vendor.BankName,
            BankAccountNumber = vendor.BankAccountNumber,
            BankAccountTitle = vendor.BankAccountTitle,
            Iban = vendor.Iban,
            SwiftCode = vendor.SwiftCode,
            PaymentTermDays = vendor.PaymentTermDays,
            PaymentTermsNotes = vendor.PaymentTermsNotes,
            Notes = vendor.Notes
        };

        return View(dto);
    }

    // POST: Vendor/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Guid companyId, UpdateVendorDto dto)
    {
        if (!ModelState.IsValid)
        {
            var company = await _companyService.GetByIdAsync(companyId);
            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            return View(dto);
        }

        try
        {
            var currentUserId = GetCurrentUserId();

            var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
            if (userCompany == null)
            {
                return NotFound();
            }

            await _vendorService.UpdateVendorAsync(id, dto);

            TempData["SuccessMessage"] = "Vendor updated successfully!";
            return RedirectToAction(nameof(Details), new { id, companyId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            var company = await _companyService.GetByIdAsync(companyId);
            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            return View(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating vendor");
            ModelState.AddModelError("", "An error occurred while updating the vendor. Please try again.");
            var company = await _companyService.GetByIdAsync(companyId);
            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            return View(dto);
        }
    }

    // POST: Vendor/Activate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, Guid companyId)
    {
        try
        {
            await _vendorService.ActivateVendorAsync(id);
            TempData["SuccessMessage"] = "Vendor activated successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating vendor");
            TempData["ErrorMessage"] = "An error occurred while activating the vendor.";
        }

        return RedirectToAction(nameof(Details), new { id, companyId });
    }

    // POST: Vendor/Deactivate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, Guid companyId)
    {
        try
        {
            await _vendorService.DeactivateVendorAsync(id);
            TempData["SuccessMessage"] = "Vendor deactivated successfully!";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating vendor");
            TempData["ErrorMessage"] = "An error occurred while deactivating the vendor.";
        }

        return RedirectToAction(nameof(Details), new { id, companyId });
    }

    // POST: Vendor/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid companyId)
    {
        try
        {
            await _vendorService.DeleteVendorAsync(id);
            TempData["SuccessMessage"] = "Vendor deleted successfully!";
            return RedirectToAction(nameof(Index), new { companyId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting vendor");
            TempData["ErrorMessage"] = "An error occurred while deleting the vendor.";
            return RedirectToAction(nameof(Details), new { id, companyId });
        }
    }

    // ===== Invoice Template Management =====

    // GET: Vendor/Templates/5?companyId=xxx
    public async Task<IActionResult> Templates(Guid id, Guid companyId)
    {
        var currentUserId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
        if (userCompany == null) return NotFound();

        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null || vendor.CompanyId != companyId) return NotFound();

        var templates = await _context.VendorInvoiceTemplates
            .Include(t => t.DefaultChartOfAccount)
            .Where(t => t.VendorId == id)
            .OrderByDescending(t => t.IsActive)
            .ThenByDescending(t => t.UpdatedAt)
            .ToListAsync();

        var company = await _companyService.GetByIdAsync(companyId);
        ViewBag.CompanyId = companyId;
        ViewBag.CompanyName = company?.Name;
        ViewBag.VendorId = id;
        ViewBag.VendorName = vendor.Name;

        return View(templates);
    }

    // GET: Vendor/CreateTemplate/5?companyId=xxx
    public async Task<IActionResult> CreateTemplate(Guid id, Guid companyId)
    {
        var currentUserId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
        if (userCompany == null) return NotFound();

        var vendor = await _context.Vendors.FindAsync(id);
        if (vendor == null || vendor.CompanyId != companyId) return NotFound();

        var company = await _companyService.GetByIdAsync(companyId);
        ViewBag.CompanyId = companyId;
        ViewBag.CompanyName = company?.Name;
        ViewBag.VendorId = id;
        ViewBag.VendorName = vendor.Name;

        await PopulateChartOfAccountsAsync(companyId);

        return View(new VendorInvoiceTemplate { VendorId = id, TemplateName = $"{vendor.Name} - Default" });
    }

    // POST: Vendor/CreateTemplate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTemplate(Guid companyId, VendorInvoiceTemplate model)
    {
        var currentUserId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
        if (userCompany == null) return NotFound();

        try
        {
            model.Id = Guid.NewGuid();
            model.CreatedAt = DateTime.UtcNow;
            model.UpdatedAt = DateTime.UtcNow;

            if (model.DefaultChartOfAccountId == Guid.Empty)
                model.DefaultChartOfAccountId = null;
            if (model.DefaultAdvanceTaxAccountId == Guid.Empty)
                model.DefaultAdvanceTaxAccountId = null;
            if (model.DefaultSalesTaxInputAccountId == Guid.Empty)
                model.DefaultSalesTaxInputAccountId = null;
            _context.VendorInvoiceTemplates.Add(model);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Invoice template created successfully!";
            return RedirectToAction(nameof(Templates), new { id = model.VendorId, companyId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice template");
            TempData["ErrorMessage"] = "Error creating template.";

            var vendor = await _context.Vendors.FindAsync(model.VendorId);
            var company = await _companyService.GetByIdAsync(companyId);
            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            ViewBag.VendorId = model.VendorId;
            ViewBag.VendorName = vendor?.Name;
            await PopulateChartOfAccountsAsync(companyId);

            return View(model);
        }
    }

    // GET: Vendor/EditTemplate/5?companyId=xxx
    public async Task<IActionResult> EditTemplate(Guid id, Guid companyId)
    {
        var currentUserId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
        if (userCompany == null) return NotFound();

        var template = await _context.VendorInvoiceTemplates
            .Include(t => t.Vendor)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null || template.Vendor?.CompanyId != companyId) return NotFound();

        var company = await _companyService.GetByIdAsync(companyId);
        ViewBag.CompanyId = companyId;
        ViewBag.CompanyName = company?.Name;
        ViewBag.VendorId = template.VendorId;
        ViewBag.VendorName = template.Vendor?.Name;

        await PopulateChartOfAccountsAsync(companyId, template.DefaultChartOfAccountId);

        return View(template);
    }

    // POST: Vendor/EditTemplate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTemplate(Guid id, Guid companyId, VendorInvoiceTemplate model)
    {
        var currentUserId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(currentUserId, companyId);
        if (userCompany == null) return NotFound();

        try
        {
            var template = await _context.VendorInvoiceTemplates
                .Include(t => t.Vendor)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null || template.Vendor?.CompanyId != companyId) return NotFound();

            template.TemplateName = model.TemplateName;
            template.HasInvoiceNumber = model.HasInvoiceNumber;
            template.HasInvoiceDate = model.HasInvoiceDate;
            template.HasDueDate = model.HasDueDate;
            template.HasDescription = model.HasDescription;
            template.HasLineItems = model.HasLineItems;
            template.HasTaxRate = model.HasTaxRate;
            template.HasSubTotal = model.HasSubTotal;
            template.HasAdvanceTaxAccount = model.HasAdvanceTaxAccount;
            template.HasSalesTaxInputAccount = model.HasSalesTaxInputAccount;
            template.InvoiceNumberLabel = model.InvoiceNumberLabel;
            template.InvoiceDateLabel = model.InvoiceDateLabel;
            template.DueDateLabel = model.DueDateLabel;
            template.SubTotalLabel = model.SubTotalLabel;
            template.TaxLabel = model.TaxLabel;
            template.TotalLabel = model.TotalLabel;
            template.AdvanceTaxAmountLabel = model.AdvanceTaxAmountLabel;
            template.SalesTaxInputAmountLabel = model.SalesTaxInputAmountLabel;
            template.DefaultTaxRate = model.DefaultTaxRate;
            template.DefaultChartOfAccountId = (model.DefaultChartOfAccountId == Guid.Empty) ? null : model.DefaultChartOfAccountId;
            template.DefaultAdvanceTaxAccountId = (model.DefaultAdvanceTaxAccountId == Guid.Empty) ? null : model.DefaultAdvanceTaxAccountId;
            template.DefaultSalesTaxInputAccountId = (model.DefaultSalesTaxInputAccountId == Guid.Empty) ? null : model.DefaultSalesTaxInputAccountId;
            template.Notes = model.Notes;
            template.IsActive = model.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Invoice template updated successfully!";
            return RedirectToAction(nameof(Templates), new { id = template.VendorId, companyId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating invoice template");
            TempData["ErrorMessage"] = "Error updating template.";

            var vendor = await _context.Vendors.FindAsync(model.VendorId);
            var company = await _companyService.GetByIdAsync(companyId);
            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            ViewBag.VendorId = model.VendorId;
            ViewBag.VendorName = vendor?.Name;
            await PopulateChartOfAccountsAsync(companyId, model.DefaultChartOfAccountId);

            return View(model);
        }
    }

    // POST: Vendor/DeleteTemplate/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTemplate(Guid id, Guid companyId)
    {
        try
        {
            var template = await _context.VendorInvoiceTemplates
                .Include(t => t.Vendor)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (template == null) return NotFound();

            var vendorId = template.VendorId;
            _context.VendorInvoiceTemplates.Remove(template);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Template deleted successfully!";
            return RedirectToAction(nameof(Templates), new { id = vendorId, companyId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting template");
            TempData["ErrorMessage"] = "Error deleting template.";
            return RedirectToAction(nameof(Index), new { companyId });
        }
    }

    private async Task PopulateChartOfAccountsAsync(Guid companyId, Guid? selectedId = null)
    {
        var accounts = await _context.ChartOfAccounts
            .Where(a => a.CompanyId == companyId && a.IsActive)
            .OrderBy(a => a.Code)
            .Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.Name}",
                Selected = selectedId.HasValue && a.Id == selectedId.Value
            })
            .ToListAsync();

        ViewBag.ChartOfAccounts = accounts;
    }
}
