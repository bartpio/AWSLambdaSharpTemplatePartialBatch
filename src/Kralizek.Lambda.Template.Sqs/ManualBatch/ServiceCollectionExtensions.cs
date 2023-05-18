using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Kralizek.Lambda.PartialBatch.ManualBatch;

/// <summary>
/// Provides extensions to register services needed to handle messages, including partial batch support.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all the services needed to handle messages of type <typeparamref name="TMessage"/>.
    /// Includes manually processed partial batch support, with the registered <see cref="IMessageHandler{TMessage}"/> being invoked once per batch.
    /// </summary>
    /// <param name="services">The collection of service registrations.</param>
    /// <param name="lifetime">The lifetime used for the <see cref="IMessageHandler{TMessage}"/> to register. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
    /// <typeparam name="TMessage">The internal type of the SNS notification.</typeparam>
    /// <typeparam name="THandler">The concrete type of the <see cref="IMessageHandler{TMessage}"/> to be registered.</typeparam>
    /// <returns>The configured collection of service registrations.</returns>
    public static IPartialBatchMessageHandlerConfigurator<TMessage> UsePartialBatchQueueMessageHandler<TMessage, THandler>(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TMessage : class
        where THandler : class, IMessageHandler<TMessage>
    {
        services.AddOptions();

        services.AddTransient<IRequestResponseHandler<SQSEvent, SQSBatchResponse>, SqsEventHandler<TMessage>>();

        services.TryAddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();

        services.Add(ServiceDescriptor.Describe(typeof(IMessageHandler<TMessage>), typeof(THandler), lifetime));

        var configurator = new MessageHandlerConfigurator<TMessage>(services);

        return configurator;
    }
}

/// <summary>
/// An interface used to represent a configurator of <see cref="IMessageHandler{TMessage}"/>,
/// with partial batch support.
/// </summary>
/// <typeparam name="TMessage">The internal type of the SQS message.</typeparam>
public interface IPartialBatchMessageHandlerConfigurator<TMessage> : IMessageHandlerConfigurator<TMessage>
    where TMessage : class
{
}

internal sealed class MessageHandlerConfigurator<TMessage> : IPartialBatchMessageHandlerConfigurator<TMessage>
    where TMessage : class
{
    public MessageHandlerConfigurator(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public IServiceCollection Services { get; }
}