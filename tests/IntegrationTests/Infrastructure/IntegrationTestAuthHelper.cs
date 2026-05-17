using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using BuildingBlocks.Web;
using Microsoft.IdentityModel.Tokens;

namespace IntegrationTests.Infrastructure;

public static class IntegrationTestAuthHelper
{
    public const string Secret = "test-jwt-secret-key-at-least-32-characters-long";
    public const string Issuer = "Microservices.Ecommerce.Tests";
    public const string Audience = "Microservices.Ecommerce.Tests";

    public static string CreateToken(Guid customerId, string role = AuthRoles.Customer) =>
        CreateToken(Guid.NewGuid(), customerId, $"{customerId:N}@test.local", role);

    public static string CreateToken(Guid userId, Guid customerId, string email, string role)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(AuthClaimTypes.CustomerId, customerId.ToString()),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static void SetBearerToken(HttpClient client, Guid customerId)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", CreateToken(customerId));
    }

    public static void ApplyJwtSettings(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.UseSetting("Jwt:Secret", Secret);
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
    }
}
