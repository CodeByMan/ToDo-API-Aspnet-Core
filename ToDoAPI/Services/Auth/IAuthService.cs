using ToDoAPI_ASPNET.Models.Response;
using ToDoAPI_ASPNET.Models.Dtos.Auth;

namespace ToDoAPI_ASPNET.Services.Auth;

public interface IAuthService
{
    Task<ApiResponse<AuthResponseDto>> RegisterAsync(AuthRegisterDto authRegister);
    Task<ApiResponse<AuthResponseDto>> LoginAsync(AuthLoginDto authLogin);
    Task<ApiResponse<AuthResponseDto>> RefreshAsync(AuthRefreshTokenDto authRefresh);
    Task<bool> LogoutAsync(AuthRefreshTokenDto authRefresh);
}
