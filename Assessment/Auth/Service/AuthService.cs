using System;
using System.Threading.Tasks;
using Assessment.Auth.Repository;
using BCrypt.Net; // BCrypt.Net-Next
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

public sealed class AuthService
{
    private readonly AdminRepo _repo;
    private readonly string _jwtKey;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;

    public AuthService(AdminRepo repo, IConfiguration config)
    {
        _repo = repo;
        _jwtKey = config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
        _jwtIssuer = config["Jwt:Issuer"] ?? "YourApp";
        _jwtAudience = config["Jwt:Audience"] ?? "YourAppClients";
    }

    public async Task<AuthResult> LoginAsync(string email, string password, string ip)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return AuthResult.Fail();

        var admin = await _repo.FindByEmailAsync(email.Trim().ToLowerInvariant());
        if (admin is null || !admin.IsActive || !BCrypt.Verify(password, admin.PasswordHash))
            return AuthResult.Fail();

        await _repo.UpdateLastLoginAsync(admin.AdminId, DateTime.UtcNow);

        // Issue JWT (15–30 min expiry recommended)
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.AdminId.ToString()),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _jwtIssuer,
            audience: _jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return AuthResult.Success(jwt);
    }
}

public record AuthResult(bool IsSuccess, string? Token)
{
    public static AuthResult Success(string token) => new(true, token);
    public static AuthResult Fail() => new(false, null);
}
