using InvoiceAutomation.Core.DTOs.Employee;
using InvoiceAutomation.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InvoiceAutomation.Controllers;

[Authorize]
public class EmployeeController : Controller
{
    private readonly IEmployeeService _employeeService;
    private readonly ICompanyService _companyService;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        IEmployeeService employeeService,
        ICompanyService companyService,
        ILogger<EmployeeController> logger)
    {
        _employeeService = employeeService;
        _companyService = companyService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    // GET: Employee/SelectCompany
    public async Task<IActionResult> SelectCompany(string? target)
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
            var action = target == "Invite" ? "Invite" : "Index";
            return RedirectToAction(action, new { companyId = companies[0].Id });
        }

        var targetAction = target == "Invite" ? "Invite" : "Index";
        ViewData["Title"] = "Select Company - Employees";
        ViewData["TargetAction"] = targetAction;
        ViewData["TargetController"] = "Employee";
        ViewData["SectionTitle"] = "Employees";
        ViewData["SectionDescription"] = target == "Invite"
            ? "Select a company to invite a team member"
            : "Select a company to manage its employees";
        ViewData["SectionIcon"] = "bi-people";

        return View(companies);
    }

    // GET: Employee?companyId=xxx
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

            var employees = await _employeeService.GetCompanyEmployeesAsync(companyId);
            var canManage = await _employeeService.CanManageEmployeesAsync(companyId, currentUserId);

            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company.Name;
            ViewBag.CanManage = canManage;
            ViewBag.CurrentUserId = currentUserId;

            return View(employees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading employees for company {CompanyId}", companyId);
            TempData["ErrorMessage"] = "An error occurred while loading employees.";
            return RedirectToAction("Index", "Company");
        }
    }

    // GET: Employee/Details?companyId=xxx&userId=yyy
    public async Task<IActionResult> Details(Guid companyId, Guid userId)
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

            var employee = await _employeeService.GetEmployeeDetailsAsync(companyId, userId);
            if (employee == null)
            {
                return NotFound();
            }

            var company = await _companyService.GetByIdAsync(companyId);
            var canManage = await _employeeService.CanManageEmployeesAsync(companyId, currentUserId);

            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            ViewBag.CanManage = canManage;
            ViewBag.CurrentUserId = currentUserId;

            return View(employee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading employee details");
            TempData["ErrorMessage"] = "An error occurred while loading employee details.";
            return RedirectToAction(nameof(Index), new { companyId });
        }
    }

    // GET: Employee/Invite?companyId=xxx
    public async Task<IActionResult> Invite(Guid companyId)
    {
        var currentUserId = GetCurrentUserId();

        if (!await _employeeService.CanManageEmployeesAsync(companyId, currentUserId))
        {
            TempData["ErrorMessage"] = "You don't have permission to invite employees.";
            return RedirectToAction(nameof(Index), new { companyId });
        }

        var company = await _companyService.GetByIdAsync(companyId);
        if (company == null)
        {
            return NotFound();
        }

        ViewBag.CompanyId = companyId;
        ViewBag.CompanyName = company.Name;

        return View(new InviteEmployeeDto { CompanyId = companyId });
    }

    // POST: Employee/Invite
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Invite(InviteEmployeeDto dto)
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

            if (!await _employeeService.CanManageEmployeesAsync(dto.CompanyId, currentUserId))
            {
                TempData["ErrorMessage"] = "You don't have permission to invite employees.";
                return RedirectToAction(nameof(Index), new { companyId = dto.CompanyId });
            }

            await _employeeService.InviteEmployeeAsync(dto, currentUserId);

            TempData["SuccessMessage"] = $"Employee invited successfully! An email has been sent to {dto.Email}.";
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
            _logger.LogError(ex, "Error inviting employee");
            ModelState.AddModelError("", "An error occurred while inviting the employee. Please try again.");
            var company = await _companyService.GetByIdAsync(dto.CompanyId);
            ViewBag.CompanyId = dto.CompanyId;
            ViewBag.CompanyName = company?.Name;
            return View(dto);
        }
    }

    // GET: Employee/UpdateRole?companyId=xxx&userId=yyy
    public async Task<IActionResult> UpdateRole(Guid companyId, Guid userId)
    {
        var currentUserId = GetCurrentUserId();

        if (!await _employeeService.CanManageEmployeesAsync(companyId, currentUserId))
        {
            TempData["ErrorMessage"] = "You don't have permission to update employee roles.";
            return RedirectToAction(nameof(Index), new { companyId });
        }

        var employee = await _employeeService.GetEmployeeDetailsAsync(companyId, userId);
        if (employee == null)
        {
            return NotFound();
        }

        var company = await _companyService.GetByIdAsync(companyId);

        ViewBag.CompanyId = companyId;
        ViewBag.CompanyName = company?.Name;
        ViewBag.EmployeeName = employee.FullName;
        ViewBag.UserId = userId;

        return View(new UpdateEmployeeRoleDto { Role = employee.Role });
    }

    // POST: Employee/UpdateRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(Guid companyId, Guid userId, UpdateEmployeeRoleDto dto)
    {
        if (!ModelState.IsValid)
        {
            var employee = await _employeeService.GetEmployeeDetailsAsync(companyId, userId);
            var company = await _companyService.GetByIdAsync(companyId);
            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            ViewBag.EmployeeName = employee?.FullName;
            ViewBag.UserId = userId;
            return View(dto);
        }

        try
        {
            var currentUserId = GetCurrentUserId();

            if (!await _employeeService.CanManageEmployeesAsync(companyId, currentUserId))
            {
                TempData["ErrorMessage"] = "You don't have permission to update employee roles.";
                return RedirectToAction(nameof(Index), new { companyId });
            }

            await _employeeService.UpdateEmployeeRoleAsync(companyId, userId, dto);

            TempData["SuccessMessage"] = "Employee role updated successfully!";
            return RedirectToAction(nameof(Details), new { companyId, userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating employee role");
            ModelState.AddModelError("", "An error occurred while updating the role. Please try again.");
            var employee = await _employeeService.GetEmployeeDetailsAsync(companyId, userId);
            var company = await _companyService.GetByIdAsync(companyId);
            ViewBag.CompanyId = companyId;
            ViewBag.CompanyName = company?.Name;
            ViewBag.EmployeeName = employee?.FullName;
            ViewBag.UserId = userId;
            return View(dto);
        }
    }

    // POST: Employee/Activate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid companyId, Guid userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            if (!await _employeeService.CanManageEmployeesAsync(companyId, currentUserId))
            {
                TempData["ErrorMessage"] = "You don't have permission to activate employees.";
                return RedirectToAction(nameof(Index), new { companyId });
            }

            await _employeeService.ActivateEmployeeAsync(companyId, userId);

            TempData["SuccessMessage"] = "Employee activated successfully!";
            return RedirectToAction(nameof(Details), new { companyId, userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating employee");
            TempData["ErrorMessage"] = "An error occurred while activating the employee.";
            return RedirectToAction(nameof(Details), new { companyId, userId });
        }
    }

    // POST: Employee/Deactivate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid companyId, Guid userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            if (!await _employeeService.CanManageEmployeesAsync(companyId, currentUserId))
            {
                TempData["ErrorMessage"] = "You don't have permission to deactivate employees.";
                return RedirectToAction(nameof(Index), new { companyId });
            }

            if (userId == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot deactivate yourself.";
                return RedirectToAction(nameof(Details), new { companyId, userId });
            }

            await _employeeService.DeactivateEmployeeAsync(companyId, userId);

            TempData["SuccessMessage"] = "Employee deactivated successfully!";
            return RedirectToAction(nameof(Details), new { companyId, userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating employee");
            TempData["ErrorMessage"] = "An error occurred while deactivating the employee.";
            return RedirectToAction(nameof(Details), new { companyId, userId });
        }
    }

    // POST: Employee/Remove
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid companyId, Guid userId)
    {
        try
        {
            var currentUserId = GetCurrentUserId();

            if (!await _employeeService.CanManageEmployeesAsync(companyId, currentUserId))
            {
                TempData["ErrorMessage"] = "You don't have permission to remove employees.";
                return RedirectToAction(nameof(Index), new { companyId });
            }

            if (userId == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot remove yourself.";
                return RedirectToAction(nameof(Details), new { companyId, userId });
            }

            await _employeeService.RemoveEmployeeAsync(companyId, userId);

            TempData["SuccessMessage"] = "Employee removed successfully!";
            return RedirectToAction(nameof(Index), new { companyId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing employee");
            TempData["ErrorMessage"] = "An error occurred while removing the employee.";
            return RedirectToAction(nameof(Details), new { companyId, userId });
        }
    }
}
