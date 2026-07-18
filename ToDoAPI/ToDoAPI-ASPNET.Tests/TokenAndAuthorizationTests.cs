using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using ToDoAPI_ASPNET.Controllers;
using ToDoAPI_ASPNET.Models.Entities;
using ToDoAPI_ASPNET.Services.Utils;

namespace ToDoAPI_ASPNET.Tests;

public class TokenAndAuthorizationTests
{
    [Fact]
    public void AccessToken_UsesConfiguredHourlyExpiryAndUserClaims()
    {
        var settings = TestSupport.CreateJwtSettings();
        var user = new User
        {
            Id = 42,
            Email = "developer@example.com",
            Username = "developer",
            Password = "not-used"
        };

        var tokenValue = TokenUtil.GenerateToken(user, settings, Token.TokenType.Access);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenValue);

        Assert.InRange(token.ValidTo - token.ValidFrom, TimeSpan.FromMinutes(59), TimeSpan.FromMinutes(61));
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == "42");
        Assert.Contains(token.Claims, claim => claim.Type == TokenUtil.TokenTypeClaim && claim.Value == "access");
    }

    [Fact]
    public void ToDoItemController_RequiresAuthorization()
    {
        var authorizeAttribute = typeof(ToDoItemController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .SingleOrDefault();

        Assert.NotNull(authorizeAttribute);
    }
}
