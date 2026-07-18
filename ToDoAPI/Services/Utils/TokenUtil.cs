using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ToDoAPI_ASPNET.Models.Config;
using ToDoAPI_ASPNET.Models.Entities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using ToDoAPI_ASPNET.Models.Dtos.Auth;

namespace ToDoAPI_ASPNET.Services.Utils;

public static class TokenUtil
{
    public const string TokenTypeClaim = "token_type";

    public static string GenerateToken(User user, JWTSettings jwt, Token.TokenType type)
    {
        var now = DateTime.UtcNow;
        var expires = type switch
        {
            Token.TokenType.Refresh => now.AddDays(jwt.RefreshTokenExpiry),
            Token.TokenType.Access => now.AddHours(jwt.AccessTokenExpiry),
            _ => now.AddMinutes(15)
        };

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                new DateTimeOffset(now).ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(TokenTypeClaim, type.ToString().ToLowerInvariant())
        };

        if (type == Token.TokenType.Access)
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        var key = new SymmetricSecurityKey(Base64UrlEncoder.DecodeBytes(jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static AuthResponseDto GenerateTokens(User user, JWTSettings jwt)
    {
        return new AuthResponseDto
        {
            Access = GenerateToken(user, jwt, Token.TokenType.Access),
            Refresh = GenerateToken(user, jwt, Token.TokenType.Refresh)
        };
    }

    public static ClaimsPrincipal? ValidateToken(string token, JWTSettings jwt)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Base64UrlEncoder.DecodeBytes(jwt.Key);

        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwt.Issuer,
                ValidateAudience = true,
                ValidAudience = jwt.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                RequireExpirationTime = true,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !string.Equals(jwtToken.Header.Alg, SecurityAlgorithms.HmacSha256, StringComparison.Ordinal))
            {
                return null;
            }

            return principal;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
