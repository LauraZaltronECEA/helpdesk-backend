using api.models.DTOs;
using api.models.Entities;
using api.models.Responses;
using api.services.Data;
using api.services.Handlers;
using api.services.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Configuration;   
    
namespace api.services.v1;

// Handles user authentication, registration, email confirmation, and password reset.
// Uses BCrypt for password hashing and the custom Role/UserRole permission system.
public class AuthService : IAuthRepository
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly EmailHandler _emailHandler;

    public AuthService(AppDbContext db, IConfiguration configuration, EmailHandler emailHandler)
    {
        _db = db;
        _configuration = configuration;
        _emailHandler = emailHandler;
    }

    // Validates the user's credentials and returns a JWT on success.
    // Checks: user exists, password matches (BCrypt), email is confirmed, account is active.
    // The role name is resolved from the UserRoles relationship, falling back to the stored Role FK.
    public async Task<LoginResponse> Login(LoginDTO dto)
    {
        var response = new LoginResponse();

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == dto.Username);

        // User not found
        if (user == null)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "Credenciales invalidas";
            return response;
        }

        // Wrong password
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "Credenciales invalidas";
            return response;
        }

        // Email not confirmed yet
        if (user.EmailConfirmed == 0)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "Debes confirmar tu correo electronico antes de iniciar sesion";
            return response;
        }

        // Account is inactive
        if (user.Active == 0)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "Usuario inactivo";
            return response;
        }

        // Update last login timestamp
        user.Last_Login = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Resolve role name: try from UserRoles relationship first, then fall back to the Role FK
        var roleName = user.UserRoles?.FirstOrDefault()?.Role?.Role_Name;

        if (string.IsNullOrEmpty(roleName))
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == user.Role);
            roleName = role?.Role_Name ?? "viewer";
        }

        // Generate JWT
        var jwt = new JwtHandler(_configuration);
        var token = jwt.GenerateToken(user, roleName);

        response.Estado = true;
        response.Codigo = 1;
        response.Mensaje = "Login exitoso";
        response.UserId = user.Id;
        response.Username = user.Username ?? string.Empty;
        response.Fullname = user.Fullname ?? string.Empty;
        response.Email = user.Email ?? string.Empty;
        response.Role = roleName;
        response.Active = user.Active;
        response.Token = token;

        return response;
    }

    // Creates a new user account, assigns the default "viewer" role,
    // generates an email confirmation token, and sends the confirmation link via SMTP.
    // If the email fails to send, the created user is rolled back.
    public async Task<GeneralResponse> Register(RegisterDTO dto, string baseUrl)
    {
        var response = new GeneralResponse();

        // Check for duplicate username
        var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (existingUser != null)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "El nombre de usuario ya existe";
            return response;
        }

        // Check for duplicate email
        var existingEmail = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingEmail != null)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "El correo electronico ya esta registrado";
            return response;
        }

        // Find the default "viewer" role
        var defaultRole = await _db.Roles.FirstOrDefaultAsync(r => r.Role_Name == "viewer");
        if (defaultRole == null)
        {
            response.Estado = false;
            response.Codigo = 500;
            response.Mensaje = "Error de configuracion: rol por defecto no encontrado";
            return response;
        }

        // Create the user
        var user = new User
        {
            Username = dto.Username,
            Fullname = dto.Fullname,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Area = dto.AreaId,
            Active = 1,
            Role = defaultRole.Id,
            EmailConfirmed = 0,
            EmailConfirmationToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        // Assign the default role
        var userRole = new UserRole
        {
            Id_User = user.Id,
            Id_Roles = defaultRole.Id
        };
        _db.UserRoles.Add(userRole);
        await _db.SaveChangesAsync();

        // Build and send the confirmation email
        var confirmationLink = $"{baseUrl}/api/v1/auth/confirm-email?userId={user.Id}&token={user.EmailConfirmationToken}";

        try
        {
            await _emailHandler.SendConfirmationEmailAsync(user.Email, user.Fullname ?? user.Username ?? "", confirmationLink);
        }
        catch (Exception)
        {
            // Roll back user creation on email failure
            _db.Users.Remove(user);
            await _db.SaveChangesAsync();

            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "No se pudo enviar el correo de confirmacion. Intenta registrarte nuevamente mas tarde.";
            return response;
        }

        response.Estado = true;
        response.Codigo = 1;
        response.Mensaje = "Usuario registrado correctamente. Revisa tu correo para confirmar la cuenta.";

        return response;
    }

    // Confirms a user's email address by matching the stored confirmation token.
    // Marks EmailConfirmed = 1 and clears the token.
    public async Task<GeneralResponse> ConfirmEmail(int userId, string token)
    {
        var response = new GeneralResponse();

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "Usuario no encontrado";
            return response;
        }

        // Already confirmed
        if (user.EmailConfirmed == 1)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "El correo ya ha sido confirmado";
            return response;
        }

        // Token mismatch
        if (user.EmailConfirmationToken != token)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "El enlace de confirmacion es invalido o ha expirado";
            return response;
        }

        user.EmailConfirmed = 1;
        user.EmailConfirmationToken = null;
        await _db.SaveChangesAsync();

        response.Estado = true;
        response.Codigo = 1;
        response.Mensaje = "Correo electronico confirmado exitosamente. Ya puedes iniciar sesion.";

        return response;
    }

    // Sends a password reset email if the email belongs to a confirmed account.
    // Generates a reset token with a 1-hour expiry.
    public async Task<GeneralResponse> ForgotPassword(ForgotPasswordDTO dto, string baseUrl)
    {
        var response = new GeneralResponse();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            // Don't reveal whether the email exists (security best practice)
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "Si el correo existe, recibiras un enlace para restablecer tu contrasena";
            return response;
        }

        if (user.EmailConfirmed == 0)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "Debes confirmar tu correo electronico antes de restablecer la contrasena";
            return response;
        }

        // Generate reset token
        user.PasswordResetToken = Guid.NewGuid().ToString("N");
        user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1);
        await _db.SaveChangesAsync();

        var email = user.Email ?? "";
        var resetLink = $"{baseUrl}/api/v1/auth/reset-password?email={Uri.EscapeDataString(email)}&token={user.PasswordResetToken}";

        try
        {
            await _emailHandler.SendPasswordResetEmailAsync(email, user.Fullname ?? user.Username ?? "", resetLink);
        }
        catch (Exception)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "No se pudo enviar el correo de restablecimiento. Intenta nuevamente mas tarde.";
            return response;
        }

        response.Estado = true;
        response.Codigo = 1;
        response.Mensaje = "Si el correo existe, recibiras un enlace para restablecer tu contrasena";

        return response;
    }

    // Resets the user's password using the token from the email.
    // Validates the token and checks expiry before updating the password.
    public async Task<GeneralResponse> ResetPassword(ResetPasswordDTO dto)
    {
        var response = new GeneralResponse();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "Solicitud invalida";
            return response;
        }

        // Token mismatch
        if (user.PasswordResetToken != dto.Token)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "El enlace de restablecimiento es invalido o ha expirado";
            return response;
        }

        // Token expired
        if (user.PasswordResetTokenExpires == null || user.PasswordResetTokenExpires < DateTime.UtcNow)
        {
            response.Estado = false;
            response.Codigo = 0;
            response.Mensaje = "El enlace de restablecimiento ha expirado. Solicita uno nuevo.";
            return response;
        }

        // Update password and clear the reset token
        user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpires = null;
        await _db.SaveChangesAsync();

        response.Estado = true;
        response.Codigo = 1;
        response.Mensaje = "Contrasena restablecida exitosamente. Ya puedes iniciar sesion con tu nueva contrasena.";

        return response;
    }
}
