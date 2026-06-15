using api.models.DTOs;
using api.models.Responses;

namespace api.services.Repositories;

public interface IAuthRepository
{
    Task<LoginResponse> Login(LoginDTO dto);
    Task<GeneralResponse> Register(RegisterDTO dto, string baseUrl);
    Task<GeneralResponse> ConfirmEmail(int userId, string token);
    Task<GeneralResponse> ForgotPassword(ForgotPasswordDTO dto, string baseUrl);
    Task<GeneralResponse> ResetPassword(ResetPasswordDTO dto);
}
