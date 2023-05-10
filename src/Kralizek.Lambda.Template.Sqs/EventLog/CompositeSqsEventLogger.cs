using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda.PartialBatch.EventLog
{
    /// <summary>
    /// Composite <see cref="ISqsEventLogger"/> that calls registered <see cref="ISqsEventLogger"/> loggers in sequence.
    /// By default, <see cref="CompositeSqsEventLogger"/> is not actually registered with DI, and <see cref="CreateWithFallback"/>
    /// is used to obtain an instance, including registered <see cref="ISqsEventLogger"/> loggers, plus <see cref="SqsEventLogger"/>.
    /// Generally, this default behavior should be left as-is.
    /// If <see cref="CompositeSqsEventLogger"/> is registerd with DI rather than using the default behavior, only
    /// <see cref="ISqsEventLogger"/> loggers actually registered with DI are used, and it may be desirable to register
    /// the <see cref="SqsEventLogger"/> implementation of <see cref="ISqsEventLogger"/> explicitly, alongside any
    /// additional custom loggers.
    /// </summary>
    public class CompositeSqsEventLogger : ISqsEventLogger
    {
        private readonly IEnumerable<ISqsEventLogger> _sehLoggers;

        internal CompositeSqsEventLogger(IEnumerable<ISqsEventLogger> sehLoggers) =>
            _sehLoggers = sehLoggers ?? throw new ArgumentNullException(nameof(sehLoggers));

        internal static CompositeSqsEventLogger CreateWithFallback(IServiceProvider serviceProvider)
        {
            var result = serviceProvider.GetService<CompositeSqsEventLogger>();
            if (result is not null)
            {
                return result;
            }

            IEnumerable<ISqsEventLogger> sehLoggers = new ISqsEventLogger[] { new SqsEventLogger() };
            sehLoggers = sehLoggers.Concat(serviceProvider.GetServices<ISqsEventLogger>());
            return new(sehLoggers);
        }

        async ValueTask ISqsEventLogger.BatchReceivedAsync(EventContext eventContext)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.BatchReceivedAsync(eventContext).ConfigureAwait(false);
            }
        }

        async ValueTask ISqsEventLogger.MessageReceivedAsync(EventContext eventContext, MessageContext messageContext)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.MessageReceivedAsync(eventContext, messageContext).ConfigureAwait(false);
            }
        }

        async ValueTask ISqsEventLogger.PartialBatchItemFailureAsync(EventContext eventContext, MessageContext messageContext, Exception exc)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.PartialBatchItemFailureAsync(eventContext, messageContext, exc).ConfigureAwait(false);
            }
        }

        async ValueTask ISqsEventLogger.MessageCompletedAsync(EventContext eventContext, MessageContext messageContext)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.MessageCompletedAsync(eventContext, messageContext).ConfigureAwait(false);
            }
        }

        async ValueTask ISqsEventLogger.BatchCompletedAsync(EventContext eventContext)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.BatchCompletedAsync(eventContext).ConfigureAwait(false);
            }
        }
    }
}
