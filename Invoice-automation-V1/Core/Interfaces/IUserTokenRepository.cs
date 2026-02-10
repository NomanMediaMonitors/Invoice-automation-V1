using InvoiceAutomation.Core.Entities;

namespace InvoiceAutomation.Core.Interfaces;

public interface IUserTokenRepository
{
    Task<UserToken?> GetByTokenHashAsync(string tokenHash);
    Task<UserToken> AddAsync(UserToken token);
    Task UpdateAsync(UserToken token);
    Task<List<UserToken>> GetUnusedTokensByUserAsync(Guid userId, TokenType tokenType);
}
