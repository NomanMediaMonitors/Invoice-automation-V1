using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Infrastructure.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly ApplicationDbContext _context;

    public CompanyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetByIdAsync(Guid id)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByNtnAsync(string ntn)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Ntn == ntn);
    }

    public async Task<List<Company>> GetAllAsync()
    {
        return await _context.Companies
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Company> AddAsync(Company company)
    {
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task UpdateAsync(Company company)
    {
        company.UpdatedAt = DateTime.UtcNow;
        _context.Companies.Update(company);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> NtnExistsAsync(string ntn)
    {
        return await _context.Companies
            .AnyAsync(c => c.Ntn == ntn);
    }

    public async Task<int> GetCountAsync()
    {
        return await _context.Companies.CountAsync();
    }
}
