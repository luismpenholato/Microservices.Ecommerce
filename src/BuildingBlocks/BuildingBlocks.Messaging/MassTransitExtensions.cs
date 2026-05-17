using BuildingBlocks.Messaging.Metrics;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BuildingBlocks.Messaging;

public static class MassTransitExtensions
{
    public static IServiceCollection AddMessageBus(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName,
        Action<IBusRegistrationConfigurator>? configureConsumers = null)
    {
        services.Configure<MessageBusOptions>(configuration.GetSection(MessageBusOptions.SectionName));
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddSingleton<IConsumerExecutionFaultHook, NoOpConsumerExecutionFaultHook>();
        services.AddScoped<IOutboxPublisher, MassTransitOutboxPublisher>();
        services.AddSingleton<IntegrationEventUnitOfWorkExecutor>();
        services.AddSingleton<IntegrationEventConsumeObserver>();

        var messageBusOptions = configuration
            .GetSection(MessageBusOptions.SectionName)
            .Get<MessageBusOptions>() ?? new MessageBusOptions();

        var rabbitHost = configuration["RabbitMq:Host"] ?? "localhost";
        var rabbitUser = configuration["RabbitMq:Username"] ?? "guest";
        var rabbitPass = configuration["RabbitMq:Password"] ?? "guest";

        services.AddMassTransit(x =>
        {
            configureConsumers?.Invoke(x);

            x.SetKebabCaseEndpointNameFormatter();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.UseMessageRetry(r => r.Interval(
                    messageBusOptions.RetryLimit,
                    TimeSpan.FromSeconds(messageBusOptions.RetryIntervalSeconds)));

                cfg.ConnectConsumeObserver(context.GetRequiredService<IntegrationEventConsumeObserver>());

                cfg.ConfigureEndpoints(context);
            });
        });

        services.AddOptions<MassTransitHostOptions>()
            .Configure(options => options.WaitUntilStarted = true);

        return services;
    }
}
