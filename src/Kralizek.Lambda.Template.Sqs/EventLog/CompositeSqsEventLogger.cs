using Amazon.Lambda.SQSEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Amazon.Lambda.SQSEvents.SQSEvent;

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

        public CompositeSqsEventLogger(IEnumerable<ISqsEventLogger> sehLoggers) =>
            _sehLoggers = sehLoggers ?? throw new ArgumentNullException(nameof(sehLoggers));

        public static CompositeSqsEventLogger CreateWithFallback(IServiceProvider serviceProvider)
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

        public async Task BatchReceivedAsync(ILogger logger, SQSEvent sqsEvent)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.BatchReceivedAsync(logger, sqsEvent).ConfigureAwait(false);
            }
        }

        public async Task MessageReceivedAsync(ILogger logger, SQSEvent sqsEvent, SQSMessage sqsMessage, int index)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.MessageReceivedAsync(logger, sqsEvent, sqsMessage, index).ConfigureAwait(false);
            }
        }

        public async Task PartialBatchItemFailureAsync(ILogger logger, SQSEvent sqsEvent, SQSMessage sqsMessage, Exception exc, int index)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.PartialBatchItemFailureAsync(logger, sqsEvent, sqsMessage, exc, index).ConfigureAwait(false);
            }
        }

        public async Task MessageCompletedAsync(ILogger logger, SQSEvent sqsEvent, SQSMessage sqsMessage, int index)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.MessageCompletedAsync(logger, sqsEvent, sqsMessage, index).ConfigureAwait(false);
            }
        }

        public async Task BatchCompletedAsync(ILogger logger, SQSEvent sqsEvent)
        {
            foreach (var entry in _sehLoggers)
            {
                await entry.BatchCompletedAsync(logger, sqsEvent).ConfigureAwait(false);
            }
        }
    }
}
