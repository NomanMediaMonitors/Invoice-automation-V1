using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface IUserCompanyRepository
{
    Task<UserCompany?> GetByIdAsync(Guid id);
    Task<UserCompany?> GetByUserAndCompanyAsync(Guid userId, Guid companyId);
    Task<UserCompany?> GetUserDefaultAsync(Guid userId);
    Task<List<UserCompany>> GetByUserIdAsync(Guid userId);
    Task<List<UserCompany>> GetByCompanyIdAsync(Guid companyId);
    Task<UserCompany> AddAsync(UserCompany userCompany);
    Task UpdateAsync(UserCompany userCompany);
    Task DeleteAsync(UserCompany userCompany);
    Task ClearUserDefaultAsync(Guid userId);
}
