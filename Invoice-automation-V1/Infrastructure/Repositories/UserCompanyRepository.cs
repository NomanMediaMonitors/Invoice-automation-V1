using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Infrastructure.Repositories;

public class UserCompanyRepository : IUserCompanyRepository
{
    private readonly ApplicationDbContext _context;

    public UserCompanyRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserCompany?> GetByIdAsync(Guid id)
    {
        return await _context.UserCompanies
            .Include(uc => uc.User)
            .Include(uc => uc.Company)
            .FirstOrDefaultAsync(uc => uc.Id == id);
    }

    public async Task<UserCompany?> GetByUserAndCompanyAsync(Guid userId, Guid companyId)
    {
        return await _context.UserCompanies
            .Include(uc => uc.User)
            .Include(uc => uc.Company)
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.CompanyId == companyId);
    }

    public async Task<UserCompany?> GetUserDefaultAsync(Guid userId)
    {
        return await _context.UserCompanies
            .Include(uc => uc.Company)
            .FirstOrDefaultAsync(uc => uc.UserId == userId && uc.IsUserDefault);
    }

    public async Task<List<UserCompany>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserCompanies
            .Include(uc => uc.Company)
            .Where(uc => uc.UserId == userId)
            .OrderByDescending(uc => uc.IsUserDefault)
            .ThenBy(uc => uc.Company.DisplayOrder)
            .ThenBy(uc => uc.Company.Name)
            .ToListAsync();
    }

    public async Task<List<UserCompany>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.UserCompanies
            .Include(uc => uc.User)
            .Where(uc => uc.CompanyId == companyId)
            .OrderBy(uc => uc.User.FullName)
            .ToListAsync();
    }

    public async Task<UserCompany> AddAsync(UserCompany userCompany)
    {
        _context.UserCompanies.Add(userCompany);
        await _context.SaveChangesAsync();
        return userCompany;
    }

    public async Task UpdateAsync(UserCompany userCompany)
    {
        userCompany.UpdatedAt = DateTime.UtcNow;
        _context.UserCompanies.Update(userCompany);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(UserCompany userCompany)
    {
        _context.UserCompanies.Remove(userCompany);
        await _context.SaveChangesAsync();
    }

    public async Task ClearUserDefaultAsync(Guid userId)
    {
        var userCompanies = await _context.UserCompanies
            .Where(uc => uc.UserId == userId && uc.IsUserDefault)
            .ToListAsync();

        foreach (var uc in userCompanies)
        {
            uc.IsUserDefault = false;
            uc.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}
