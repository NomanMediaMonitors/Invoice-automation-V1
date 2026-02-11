using InvoiceAutomation.Core.DTOs.Employee;
using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface IEmployeeService
{
    // Get employees for a company
    Task<List<EmployeeListDto>> GetCompanyEmployeesAsync(Guid companyId);
    Task<EmployeeDetailsDto?> GetEmployeeDetailsAsync(Guid companyId, Guid userId);

    // Invite new employee
    Task<bool> InviteEmployeeAsync(InviteEmployeeDto dto, Guid invitedByUserId);

    // Role management
    Task UpdateEmployeeRoleAsync(Guid companyId, Guid userId, UpdateEmployeeRoleDto dto);

    // Employee status
    Task ActivateEmployeeAsync(Guid companyId, Guid userId);
    Task DeactivateEmployeeAsync(Guid companyId, Guid userId);
    Task RemoveEmployeeAsync(Guid companyId, Guid userId);

    // Validation
    Task<bool> IsUserInCompanyAsync(Guid companyId, Guid userId);
    Task<bool> CanManageEmployeesAsync(Guid companyId, Guid currentUserId);
}
