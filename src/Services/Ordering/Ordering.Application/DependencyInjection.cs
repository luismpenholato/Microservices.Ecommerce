using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Ordering.Application.Orders.Handlers;

namespace Ordering.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddOrderingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddScoped<PaymentApprovedHandler>();
        services.AddScoped<PaymentRejectedHandler>();
        services.AddScoped<StockReservedHandler>();
        services.AddScoped<StockReservationFailedHandler>();

        return services;
    }
}
