using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface IChartOfAccountRepository
{
    Task<ChartOfAccount?> GetByIdAsync(Guid id);
    Task<ChartOfAccount?> GetByRecnoAsync(Guid companyId, int recno);
    Task<List<ChartOfAccount>> GetByCompanyIdAsync(Guid companyId);
    Task<List<ChartOfAccount>> GetExpenseAccountsAsync(Guid companyId);
    Task<List<ChartOfAccount>> GetBankCashAccountsAsync(Guid companyId);
    Task<ChartOfAccount> AddAsync(ChartOfAccount account);
    Task UpdateAsync(ChartOfAccount account);
    Task DeleteByCompanyIdAsync(Guid companyId);
    Task<int> GetCountByCompanyIdAsync(Guid companyId);
}
