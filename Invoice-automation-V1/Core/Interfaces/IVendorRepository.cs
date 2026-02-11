using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id);
    Task<List<Vendor>> GetByCompanyIdAsync(Guid companyId);
    Task<List<Vendor>> GetActiveByCompanyIdAsync(Guid companyId);
    Task<Vendor?> GetByEmailAsync(Guid companyId, string email);
    Task<Vendor> AddAsync(Vendor vendor);
    Task UpdateAsync(Vendor vendor);
    Task DeleteAsync(Vendor vendor);
    Task<bool> ExistsAsync(Guid companyId, string email);
    Task<int> GetCountByCompanyIdAsync(Guid companyId);
}
