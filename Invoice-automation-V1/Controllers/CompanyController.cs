using InvoiceAutomation.Core.DTOs.Company;
using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceAutomation.Controllers;

[Authorize]
public class CompanyController : Controller
{
    private readonly ICompanyService _companyService;
    private readonly IIndraajSyncService _indraajSyncService;
    private readonly ILogger<CompanyController> _logger;

    public CompanyController(
        ICompanyService companyService,
        IIndraajSyncService indraajSyncService,
        ILogger<CompanyController> logger)
    {
        _companyService = companyService;
        _indraajSyncService = indraajSyncService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    // GET: Company
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        var companies = await _companyService.GetUserCompaniesAsync(userId);
        return View(companies);
    }

    // GET: Company/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Company/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCompanyDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            var userId = GetCurrentUserId();
            var company = await _companyService.CreateAsync(dto, userId);

            TempData["SuccessMessage"] = $"Company '{company.Name}' created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating company");
            ModelState.AddModelError("", "An error occurred while creating the company. Please try again.");
            return View(dto);
        }
    }

    // GET: Company/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var userId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(userId, id);

        if (userCompany == null)
        {
            return NotFound();
        }

        // Only Admin or SuperAdmin can edit
        if (userCompany.Role != UserRole.Admin && userCompany.Role != UserRole.SuperAdmin)
        {
            TempData["ErrorMessage"] = "You don't have permission to edit this company.";
            return RedirectToAction(nameof(Index));
        }

        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        var dto = new UpdateCompanyDto
        {
            Name = company.Name,
            Ntn = company.Ntn,
            Strn = company.Strn,
            Address = company.Address,
            Phone = company.Phone,
            Email = company.Email
        };

        return View(dto);
    }

    // POST: Company/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UpdateCompanyDto dto)
    {
        if (!ModelState.IsValid)
        {
            return View(dto);
        }

        try
        {
            var userId = GetCurrentUserId();
            var userCompany = await _companyService.GetUserCompanyAsync(userId, id);

            if (userCompany == null)
            {
                return NotFound();
            }

            if (userCompany.Role != UserRole.Admin && userCompany.Role != UserRole.SuperAdmin)
            {
                TempData["ErrorMessage"] = "You don't have permission to edit this company.";
                return RedirectToAction(nameof(Index));
            }

            await _companyService.UpdateAsync(id, dto);

            TempData["SuccessMessage"] = "Company updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("", ex.Message);
            return View(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating company");
            ModelState.AddModelError("", "An error occurred while updating the company. Please try again.");
            return View(dto);
        }
    }

    // GET: Company/Details/5
    public async Task<IActionResult> Details(Guid id)
    {
        var userId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(userId, id);

        if (userCompany == null)
        {
            return NotFound();
        }

        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        ViewBag.UserRole = userCompany.Role;
        ViewBag.CanSync = await _indraajSyncService.CanSyncAsync(id);
        ViewBag.DaysSinceSync = await _indraajSyncService.GetDaysSinceLastSyncAsync(id);

        return View(company);
    }

    // GET: Company/ConnectIndraaj/5
    public async Task<IActionResult> ConnectIndraaj(Guid id)
    {
        var userId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(userId, id);

        if (userCompany == null)
        {
            return NotFound();
        }

        if (userCompany.Role != UserRole.Admin && userCompany.Role != UserRole.SuperAdmin)
        {
            TempData["ErrorMessage"] = "You don't have permission to manage Indraaj connection.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        ViewBag.CompanyName = company.Name;
        ViewBag.CompanyId = id;

        return View();
    }

    // POST: Company/ConnectIndraaj/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConnectIndraaj(Guid id, ConnectIndraajDto dto)
    {
        if (!ModelState.IsValid)
        {
            var company = await _companyService.GetByIdAsync(id);
            ViewBag.CompanyName = company?.Name;
            ViewBag.CompanyId = id;
            return View(dto);
        }

        try
        {
            var userId = GetCurrentUserId();
            var userCompany = await _companyService.GetUserCompanyAsync(userId, id);

            if (userCompany == null)
            {
                return NotFound();
            }

            if (userCompany.Role != UserRole.Admin && userCompany.Role != UserRole.SuperAdmin)
            {
                TempData["ErrorMessage"] = "You don't have permission to manage Indraaj connection.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var success = await _companyService.ConnectIndraajAsync(id, dto);

            if (success)
            {
                TempData["SuccessMessage"] = "Indraaj connection established successfully! You can now sync Chart of Accounts.";
                return RedirectToAction(nameof(Details), new { id });
            }
            else
            {
                ModelState.AddModelError("", "Failed to validate the access token. Please check and try again.");
                var company = await _companyService.GetByIdAsync(id);
                ViewBag.CompanyName = company?.Name;
                ViewBag.CompanyId = id;
                return View(dto);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting Indraaj for company {CompanyId}", id);
            ModelState.AddModelError("", "An error occurred while connecting to Indraaj. Please try again.");
            var company = await _companyService.GetByIdAsync(id);
            ViewBag.CompanyName = company?.Name;
            ViewBag.CompanyId = id;
            return View(dto);
        }
    }

    // POST: Company/SyncCoa/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncCoa(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var userCompany = await _companyService.GetUserCompanyAsync(userId, id);

            if (userCompany == null)
            {
                return NotFound();
            }

            // Check if can sync
            var canSync = await _indraajSyncService.CanSyncAsync(id);
            if (!canSync)
            {
                TempData["ErrorMessage"] = "Cannot sync now. Last sync was less than 7 days ago.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var result = await _indraajSyncService.SyncChartOfAccountsAsync(id);

            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Chart of Accounts synced successfully! Total: {result.TotalAccounts}, New: {result.NewAccounts}, Updated: {result.UpdatedAccounts}";
            }
            else
            {
                TempData["ErrorMessage"] = $"Sync failed: {result.Error}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing COA for company {CompanyId}", id);
            TempData["ErrorMessage"] = "An error occurred while syncing Chart of Accounts. Please try again.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    // POST: Company/SetDefault/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _companyService.SetUserDefaultCompanyAsync(userId, id);

            TempData["SuccessMessage"] = "Default company updated successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default company");
            TempData["ErrorMessage"] = "An error occurred while setting default company. Please try again.";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: Company/ViewChartOfAccounts/5
    public async Task<IActionResult> ViewChartOfAccounts(Guid id)
    {
        var userId = GetCurrentUserId();
        var userCompany = await _companyService.GetUserCompanyAsync(userId, id);

        if (userCompany == null)
        {
            return NotFound();
        }

        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        var accounts = await _indraajSyncService.GetLocalChartOfAccountsAsync(id);

        ViewBag.CompanyName = company.Name;
        ViewBag.CompanyId = id;

        return View(accounts);
    }
}
