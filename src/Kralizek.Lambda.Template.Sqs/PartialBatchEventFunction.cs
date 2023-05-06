using System.Diagnostics.CodeAnalysis;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda.PartialBatch
{
    /// <summary>
    /// A base class used for event functions with partial batch support.
    /// </summary>
    public abstract class PartialBatchEventFunction : RequestResponseFunction<SQSEvent, SQSBatchResponse>
    {
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "stub to prevent incorrect usage")]
        [SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "stub to prevent incorrect usage")]
        private new void RegisterHandler<THandler>(IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient) where THandler : class, IEventHandler<SQSEvent>
        {
        }
    }

    /// <summary>
    /// A base class used for event functions with partial batch support.
    /// </summary>
    /// <typeparam name="TInput">The type of the incoming request. Generally <see cref="SQSEvent"/>.</typeparam>
    public abstract class PartialBatchEventFunction<TInput> : RequestResponseFunction<TInput, SQSBatchResponse>
    {
        /// <summary>
        /// Registers the non-SQS handler for the request of type <typeparamref name="TInput"/>.
        /// This method is not typically used. It may be applicable when an intermediate abstract class deriving from
        /// <see cref="PartialBatchEventFunction{TInput}"/> is used to provide base functionality for SQS-triggered functions
        /// (in which case <typeparamref name="TInput"/> is <see cref="SQSEvent"/>, as well as for non-SQS-triggered functions
        /// (in which case <typeparamref name="TInput"/> is something else).
        /// </summary>
        /// <param name="services">The collections of services.</param>
        /// <param name="lifetime">The lifetime of the handler. Defaults to <see cref="ServiceLifetime.Transient"/>.</param>
        /// <typeparam name="THandler">The type of the handler for requests of type <typeparamref name="TInput"/>.</typeparam>
        protected new void RegisterHandler<THandler>(IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Transient) where THandler : class, IEventHandler<TInput>
        {
            services.Add(ServiceDescriptor.Describe(typeof(IEventHandler<TInput>), typeof(THandler), lifetime));
            services.Add(ServiceDescriptor.Describe(typeof(IRequestResponseHandler<TInput, SQSBatchResponse>), typeof(EventHandlerAdapter), lifetime));
        }

        internal sealed class EventHandlerAdapter : IRequestResponseHandler<TInput, SQSBatchResponse>
        {
            private readonly IEventHandler<TInput> _wrapped;

            public EventHandlerAdapter(IEventHandler<TInput> wrapped) =>
                _wrapped = wrapped ?? throw new ArgumentNullException(nameof(wrapped));

            public async Task<SQSBatchResponse> HandleAsync(TInput? input, ILambdaContext context)
            {
                await _wrapped.HandleAsync(input, context).ConfigureAwait(false);
                return new();
            }
        }
    }
}
