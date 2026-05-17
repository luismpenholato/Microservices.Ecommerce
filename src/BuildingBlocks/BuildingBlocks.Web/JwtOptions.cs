namespace BuildingBlocks.Web;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "Microservices.Ecommerce";
    public string Audience { get; set; } = "Microservices.Ecommerce";
    public int ExpirationMinutes { get; set; } = 60;
}
