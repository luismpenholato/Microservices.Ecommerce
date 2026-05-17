using ApiGateway;
using BuildingBlocks.Observability;
using BuildingBlocks.Web;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
builder.AddObservability("ApiGateway");

builder.Services.AddEcommerceJwtAuthenticationWithoutFallbackPolicy(builder.Configuration);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
builder.Services.AddHealthChecks()
    .AddCheck("gateway", () => HealthCheckResult.Healthy("YARP configuration loaded."), tags: ["ready"]);

var app = builder.Build();
app.UseObservability();
app.UseAuthentication();
app.UseMiddleware<GatewayAuthorizationMiddleware>();
app.MapReverseProxy();
app.MapGet("/", () => Results.Ok(new
{
    service = "Microservices.Ecommerce.ApiGateway",
    routes = new[] { "/identity/*", "/catalog/*", "/basket/*", "/ordering/*", "/inventory/*" }
}));
app.Run();

public partial class Program;
