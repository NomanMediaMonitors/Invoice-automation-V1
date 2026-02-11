using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Infrastructure.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly ApplicationDbContext _context;

    public VendorRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Vendor?> GetByIdAsync(Guid id)
    {
        return await _context.Vendors
            .Include(v => v.Company)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<List<Vendor>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.Vendors
            .Where(v => v.CompanyId == companyId)
            .OrderBy(v => v.Name)
            .ToListAsync();
    }

    public async Task<List<Vendor>> GetActiveByCompanyIdAsync(Guid companyId)
    {
        return await _context.Vendors
            .Where(v => v.CompanyId == companyId && v.IsActive)
            .OrderBy(v => v.Name)
            .ToListAsync();
    }

    public async Task<Vendor?> GetByEmailAsync(Guid companyId, string email)
    {
        return await _context.Vendors
            .FirstOrDefaultAsync(v => v.CompanyId == companyId &&
                                    v.Email.ToLower() == email.ToLower());
    }

    public async Task<Vendor> AddAsync(Vendor vendor)
    {
        _context.Vendors.Add(vendor);
        await _context.SaveChangesAsync();
        return vendor;
    }

    public async Task UpdateAsync(Vendor vendor)
    {
        vendor.UpdatedAt = DateTime.UtcNow;
        _context.Vendors.Update(vendor);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Vendor vendor)
    {
        _context.Vendors.Remove(vendor);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid companyId, string email)
    {
        return await _context.Vendors
            .AnyAsync(v => v.CompanyId == companyId &&
                         v.Email.ToLower() == email.ToLower());
    }

    public async Task<int> GetCountByCompanyIdAsync(Guid companyId)
    {
        return await _context.Vendors
            .CountAsync(v => v.CompanyId == companyId);
    }
}
