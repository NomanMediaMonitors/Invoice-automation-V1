using InvoiceAutomation.Core.DTOs.Vendor;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceAutomation.Controllers;

[Authorize]
public class VendorController : Controller
{
    private readonly IVendorService _vendorService;
    private readonly ICompanyService _companyService;
    private readonly ILogger<VendorController> _logger;

    public VendorController(
        IVendorService vendorService,
        ICompanyService companyService,
        ILogger<VendorController> logger)
    {
        _vendorService = vendorService;
        _companyService = companyService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    // GET: Vendor?companyId=xxx
    public async Task<IActionResult> Index(Guid companyId)
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

            var company = await _companyService.GetByIdAsync(companyId);
            if (company == null)
            {
                return NotFound();
            }

            var vendors = await _vendorService.GetCompanyVendorsAsync(companyId);

            ViewBag.CompanyId = companyId;
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
}
