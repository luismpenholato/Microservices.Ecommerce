using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Web;

public static class WebApplicationExtensions
{
    public static IServiceCollection AddBuildingBlocksWeb(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationIdAccessor, HttpCorrelationIdAccessor>();
        services.AddScoped<ICurrentUserAccessor, HttpCurrentUserAccessor>();
        return services;
    }

    public static WebApplication UseBuildingBlocksWeb(this WebApplication app)
    {
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        return app;
    }

    public static WebApplication UseEcommerceAuthentication(this WebApplication app)
    {
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }
}
