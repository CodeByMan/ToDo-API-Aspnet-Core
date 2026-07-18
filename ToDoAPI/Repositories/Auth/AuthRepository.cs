using ToDoAPI_ASPNET.Data;
using Microsoft.EntityFrameworkCore;
using ToDoAPI_ASPNET.Models.Entities;
using ToDoAPI_ASPNET.Services.Utils;

namespace ToDoAPI_ASPNET.Repositories.Auth;

public class AuthRepository(
    DataContext context
) : IAuthRepository
{
    public async Task<bool> IsUserExistsByCredentialAsync(
        string email,
        string? username = null)
    {
        return await context.Users
            .Where(u => u.Email == email || u.Username == username)
            .AnyAsync();
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await context.Users.SingleOrDefaultAsync(u => u.Email == email);
    }

    public async Task AddUserAsync(User user)
    {
        await context.Users.AddAsync(user);
        await context.SaveChangesAsync();
    }

    public async Task SaveRefreshTokenAsync(
        User user,
        string refreshToken,
        DateTime expiresAt,
        Token.TokenType type)
    {
        var token = new Token
        {
            User = user,
            UserId = user.Id,
            Value = TokenUtil.HashToken(refreshToken),
            ExpiresAt = expiresAt,
            Type = type
        };

        user.Tokens.Add(token);
        await context.Tokens.AddAsync(token);
        await context.SaveChangesAsync();
    }

    public async Task<bool> TryRevokeTokenAsync(int tokenId)
    {
        var affectedRows = await context.Tokens
            .Where(token => token.Id == tokenId && !token.isRevoked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(token => token.isRevoked, true));

        return affectedRows == 1;
    }

    public async Task<Token?> GetTokenByRefreshAsync(string refreshToken)
    {
        var tokenHash = TokenUtil.HashToken(refreshToken);

        return await context.Tokens
            .Include(t => t.User)
            .SingleOrDefaultAsync(t => t.Value == tokenHash);
    }
}
