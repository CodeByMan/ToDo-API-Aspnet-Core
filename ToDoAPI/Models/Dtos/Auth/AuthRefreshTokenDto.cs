using System.ComponentModel.DataAnnotations;

namespace ToDoAPI_ASPNET.Models.Dtos.Auth;

public class AuthRefreshTokenDto
{
    [Required(ErrorMessage = "Refresh token is required.")]
    [MinLength(20, ErrorMessage = "Refresh token is invalid.")]
    public string Token { get; init; } = string.Empty;
}
