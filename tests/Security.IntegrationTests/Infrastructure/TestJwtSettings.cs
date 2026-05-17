namespace Security.IntegrationTests.Infrastructure;

public static class TestJwtSettings
{
    public const string Secret = "test-jwt-secret-key-at-least-32-characters-long";
    public const string Issuer = "Microservices.Ecommerce.Tests";
    public const string Audience = "Microservices.Ecommerce.Tests";
}
