using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id);
    Task<Company?> GetByNtnAsync(string ntn);
    Task<List<Company>> GetAllAsync();
    Task<Company> AddAsync(Company company);
    Task UpdateAsync(Company company);
    Task<bool> NtnExistsAsync(string ntn);
    Task<int> GetCountAsync();
}
