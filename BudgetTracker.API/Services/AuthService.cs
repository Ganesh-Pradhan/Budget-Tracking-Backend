using BudgetTracker.API.Data;
using BudgetTracker.API.DTOs;
using BudgetTracker.API.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace BudgetTracker.API.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(SignupDto dto);
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<AuthResponseDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    bool VerifyPassword(string password, string passwordHash);
    string HashPassword(string password);
}

public class AuthService : IAuthService
{
    // Used to keep login timing roughly constant when the submitted email
    // does not exist, reducing user-enumeration via timing differences.
    private static readonly string DummyPasswordHash =
        BCrypt.Net.BCrypt.HashPassword("not-a-real-password");

    private readonly BudgetTrackerDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(BudgetTrackerDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    public async Task<AuthResponseDto> RegisterAsync(SignupDto dto)
    {
        var emailExists = await _context.Users
            .AnyAsync(u => u.Email == dto.Email);

        if (emailExists)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            PasswordHash = HashPassword(dto.Password),
            Currency = "INR",
            CreatedDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == dto.Email.Trim().ToLowerInvariant());

        if (user == null)
        {
            // Keep a comparable code path for unknown emails.
            BCrypt.Net.BCrypt.Verify(dto.Password, DummyPasswordHash);
            return null;
        }

        if (!VerifyPassword(dto.Password, user.PasswordHash))
        {
            return null;
        }

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponseDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return null;
        }

        user.FullName = dto.FullName.Trim();
        user.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return GenerateAuthResponse(user);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return BCrypt.Net.BCrypt.Verify(password, passwordHash);
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    private AuthResponseDto GenerateAuthResponse(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key is not configured."));

        var issuer = _configuration["Jwt:Issuer"] ?? "BudgetTrackerAPI";
        var audience = _configuration["Jwt:Audience"] ?? "BudgetTrackerClient";

        if (!int.TryParse(_configuration["Jwt:ExpiryHours"], out var expiryHours) ||
            expiryHours < 1 || expiryHours > 168)
        {
            expiryHours = 24;
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            }),
            Expires = DateTime.UtcNow.AddHours(expiryHours),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new AuthResponseDto
        {
            Token = tokenHandler.WriteToken(token),
            UserId = user.UserId,
            FullName = user.FullName,
            Email = user.Email,
            ExpiresAt = tokenDescriptor.Expires.Value,
        };
    }
}
