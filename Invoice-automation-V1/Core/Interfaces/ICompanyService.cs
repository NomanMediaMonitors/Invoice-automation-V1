using InvoiceAutomation.Core.DTOs.Company;
using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface ICompanyService
{
    // Company CRUD
    Task<Company?> GetByIdAsync(Guid id);
    Task<Company?> GetByNtnAsync(string ntn);
    Task<List<Company>> GetAllAsync();
    Task<Company> CreateAsync(CreateCompanyDto dto, Guid createdByUserId);
    Task<Company> UpdateAsync(Guid id, UpdateCompanyDto dto);
    Task<bool> NtnExistsAsync(string ntn);

    // User-Company relationships
    Task<List<CompanyDropdownDto>> GetUserCompaniesAsync(Guid userId);
    Task<Company?> GetUserDefaultCompanyAsync(Guid userId);
    Task SetUserDefaultCompanyAsync(Guid userId, Guid companyId);
    Task<UserCompany?> GetUserCompanyAsync(Guid userId, Guid companyId);
    Task AddUserToCompanyAsync(Guid userId, Guid companyId, UserRole role);
    Task UpdateUserRoleAsync(Guid userId, Guid companyId, UserRole role);
    Task RemoveUserFromCompanyAsync(Guid userId, Guid companyId);
    Task<List<UserCompany>> GetCompanyUsersAsync(Guid companyId);

    // Indraaj integration
    Task<bool> ConnectIndraajAsync(Guid companyId, ConnectIndraajDto dto);
    Task<bool> TestIndraajConnectionAsync(Guid companyId);
}
