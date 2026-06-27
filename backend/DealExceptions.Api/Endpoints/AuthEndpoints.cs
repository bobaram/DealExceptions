using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DealExceptions.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/api/auth/login", (LoginRequest req, IConfiguration config) =>
        {
            var users = config.GetSection("Jwt:Users").Get<UserConfig[]>() ?? [];
            var match = users.FirstOrDefault(u =>
                string.Equals(u.Username, req.Username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == req.Password);

            if (match is null)
                return Results.Unauthorized();

            var key    = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
            var creds  = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiry = DateTime.UtcNow.AddMinutes(config.GetValue("Jwt:ExpiryMinutes", 480));

            var token = new JwtSecurityToken(
                claims: [
                    new Claim(ClaimTypes.NameIdentifier, match.Username),
                    new Claim(ClaimTypes.Name, match.DisplayName),
                ],
                expires: expiry,
                signingCredentials: creds);

            return Results.Ok(new
            {
                token       = new JwtSecurityTokenHandler().WriteToken(token),
                displayName = match.DisplayName,
                username    = match.Username,
                expiresAt   = expiry,
            });
        })
        .AllowAnonymous();
    }

    public record LoginRequest(string Username, string Password);

    private sealed class UserConfig
    {
        public string Username    { get; init; } = "";
        public string Password    { get; init; } = "";
        public string DisplayName { get; init; } = "";
    }
}
