using api.models.DTOs;
using api.models.Responses;

namespace api.services.Repositories;

// Defines authentication and user-management operations.
public interface IAuthRepository
{
    // Validates credentials and returns a JWT on success.
    Task<LoginResponse> Login(LoginDTO dto);

    // Creates a new user account and sends a confirmation email.
    // baseUrl: The base URL of the API, used to build the confirmation link.
    Task<GeneralResponse> Register(RegisterDTO dto, string baseUrl);

    // Confirms a user's email address using the token from the confirmation link.
    Task<GeneralResponse> ConfirmEmail(int userId, string token);

    // Sends a password reset email if the email belongs to a confirmed account.
    Task<GeneralResponse> ForgotPassword(ForgotPasswordDTO dto, string baseUrl);

    // Resets the user's password using the token and new password.
    Task<GeneralResponse> ResetPassword(ResetPasswordDTO dto);
}
