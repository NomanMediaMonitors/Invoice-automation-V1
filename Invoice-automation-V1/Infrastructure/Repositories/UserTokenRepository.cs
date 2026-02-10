using InvoiceAutomation.Core.Entities;
using InvoiceAutomation.Core.Interfaces;
using InvoiceAutomation.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InvoiceAutomation.Infrastructure.Repositories;

public class UserTokenRepository : IUserTokenRepository
{
    private readonly ApplicationDbContext _context;

    public UserTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserToken?> GetByTokenHashAsync(string tokenHash)
    {
        return await _context.UserTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);
    }

    public async Task<UserToken> AddAsync(UserToken token)
    {
        _context.UserTokens.Add(token);
        await _context.SaveChangesAsync();
        return token;
    }

    public async Task UpdateAsync(UserToken token)
    {
        _context.UserTokens.Update(token);
        await _context.SaveChangesAsync();
    }

    public async Task<List<UserToken>> GetUnusedTokensByUserAsync(Guid userId, TokenType tokenType)
    {
        return await _context.UserTokens
            .Where(t => t.UserId == userId &&
                       t.TokenType == tokenType &&
                       t.UsedAt == null &&
                       t.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();
    }
}
