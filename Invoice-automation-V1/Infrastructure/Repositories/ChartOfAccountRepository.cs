using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Infrastructure.Repositories;

public class ChartOfAccountRepository : IChartOfAccountRepository
{
    private readonly ApplicationDbContext _context;

    public ChartOfAccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChartOfAccount?> GetByIdAsync(Guid id)
    {
        return await _context.ChartOfAccounts
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ChartOfAccount?> GetByRecnoAsync(Guid companyId, int recno)
    {
        return await _context.ChartOfAccounts
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Recno == recno);
    }

    public async Task<List<ChartOfAccount>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.ChartOfAccounts
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<List<ChartOfAccount>> GetExpenseAccountsAsync(Guid companyId)
    {
        return await _context.ChartOfAccounts
            .Where(c => c.CompanyId == companyId &&
                   c.AccountType != null &&
                   (c.AccountType.Contains("Expense", StringComparison.OrdinalIgnoreCase) ||
                    c.AccountType.Contains("Cost", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<List<ChartOfAccount>> GetBankCashAccountsAsync(Guid companyId)
    {
        return await _context.ChartOfAccounts
            .Where(c => c.CompanyId == companyId &&
                   c.AccountType != null &&
                   (c.AccountType.Contains("Bank", StringComparison.OrdinalIgnoreCase) ||
                    c.AccountType.Contains("Cash", StringComparison.OrdinalIgnoreCase)))
            .OrderBy(c => c.Code)
            .ToListAsync();
    }

    public async Task<ChartOfAccount> AddAsync(ChartOfAccount account)
    {
        _context.ChartOfAccounts.Add(account);
        await _context.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(ChartOfAccount account)
    {
        account.UpdatedAt = DateTime.UtcNow;
        _context.ChartOfAccounts.Update(account);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteByCompanyIdAsync(Guid companyId)
    {
        var accounts = await _context.ChartOfAccounts
            .Where(c => c.CompanyId == companyId)
            .ToListAsync();

        _context.ChartOfAccounts.RemoveRange(accounts);
        await _context.SaveChangesAsync();
    }

    public async Task<int> GetCountByCompanyIdAsync(Guid companyId)
    {
        return await _context.ChartOfAccounts
            .CountAsync(c => c.CompanyId == companyId);
    }
}
