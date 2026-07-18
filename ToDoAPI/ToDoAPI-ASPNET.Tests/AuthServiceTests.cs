using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ToDoAPI_ASPNET.Models.Dtos.Auth;
using ToDoAPI_ASPNET.Models.Utils;
using ToDoAPI_ASPNET.Repositories.Auth;
using ToDoAPI_ASPNET.Services.Auth;
using ToDoAPI_ASPNET.Services.Utils;

namespace ToDoAPI_ASPNET.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUserAndStoresOnlyHashedRefreshToken()
    {
        using var database = new TestDatabase();
        var service = CreateService(database);

        var response = await service.RegisterAsync(CreateRegistration());

        Assert.Equal("success", response.Status);
        Assert.NotNull(response.Data);
        Assert.False(string.IsNullOrWhiteSpace(response.Data.Access));
        Assert.False(string.IsNullOrWhiteSpace(response.Data.Refresh));

        var storedToken = await database.Context.Tokens.SingleAsync();
        Assert.Equal(TokenUtil.HashToken(response.Data.Refresh), storedToken.Value);
        Assert.NotEqual(response.Data.Refresh, storedToken.Value);
        Assert.False(storedToken.isRevoked);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsNewTokenPair()
    {
        using var database = new TestDatabase();
        var service = CreateService(database);
        await service.RegisterAsync(CreateRegistration());

        var response = await service.LoginAsync(new AuthLoginDto
        {
            Email = "developer@example.com",
            Password = "StrongPass123!"
        });

        Assert.Equal("success", response.Status);
        Assert.NotNull(response.Data);
        Assert.Equal(2, await database.Context.Tokens.CountAsync());
    }

    [Fact]
    public async Task RefreshAsync_RotatesTokenAndPreventsReuse()
    {
        using var database = new TestDatabase();
        var service = CreateService(database);
        var registration = await service.RegisterAsync(CreateRegistration());
        var originalRefreshToken = registration.Data!.Refresh;

        var refreshed = await service.RefreshAsync(new AuthRefreshTokenDto
        {
            Token = originalRefreshToken
        });

        Assert.Equal("success", refreshed.Status);
        Assert.NotNull(refreshed.Data);
        Assert.NotEqual(originalRefreshToken, refreshed.Data.Refresh);

        var originalHash = TokenUtil.HashToken(originalRefreshToken);
        var originalStoredToken = await database.Context.Tokens
            .SingleAsync(token => token.Value == originalHash);
        Assert.True(originalStoredToken.isRevoked);

        var reused = await service.RefreshAsync(new AuthRefreshTokenDto
        {
            Token = originalRefreshToken
        });

        Assert.Equal("error", reused.Status);
        Assert.Equal(Error.ErrorType.Unauthorized, reused.ErrorType);
    }

    [Fact]
    public async Task LogoutAsync_RevokesValidRefreshToken()
    {
        using var database = new TestDatabase();
        var service = CreateService(database);
        var registration = await service.RegisterAsync(CreateRegistration());

        var result = await service.LogoutAsync(new AuthRefreshTokenDto
        {
            Token = registration.Data!.Refresh
        });

        Assert.True(result);
        Assert.True((await database.Context.Tokens.SingleAsync()).isRevoked);
    }

    private static AuthService CreateService(TestDatabase database)
    {
        return new AuthService(
            new AuthRepository(database.Context),
            TestSupport.CreateMapper(),
            TestSupport.CreateJwtSettings(),
            NullLogger<AuthService>.Instance,
            database.Context
        );
    }

    private static AuthRegisterDto CreateRegistration()
    {
        return new AuthRegisterDto
        {
            Email = "developer@example.com",
            Username = "developer",
            FirstName = "Muhammad",
            LastName = "Nawaz",
            Password = "StrongPass123!",
            RePassword = "StrongPass123!"
        };
    }
}
