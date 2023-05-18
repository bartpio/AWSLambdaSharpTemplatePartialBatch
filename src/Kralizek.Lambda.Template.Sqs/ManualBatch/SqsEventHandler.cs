using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Kralizek.Lambda.PartialBatch.EventLog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static Amazon.Lambda.SQSEvents.SQSBatchResponse;

namespace Kralizek.Lambda.PartialBatch.ManualBatch;

/// <summary>
/// An implementation of <see cref="IRequestResponseHandler{TInput, TOutput}"/>, specialized for <see cref="SQSEvent"/> and <see cref="SQSBatchResponse"/>,
/// to support sequential processing with partial batch responses.
/// </summary>
/// <typeparam name="TMessage">The internal type of the SQS message.</typeparam>
public class SqsEventHandler<TMessage> : IRequestResponseHandler<SQSEvent, SQSBatchResponse> where TMessage : class
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructs an instance. Generally invoked using DI from the Kralizek Function framework.
    /// </summary>
    public SqsEventHandler(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory?.CreateLogger("SqsEventHandler") ?? throw new ArgumentNullException(nameof(loggerFactory));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Handles the <see cref="SQSEvent"/> by deserializing each record in sequence, then calling a message handler implementation for the entire batch of messages.
    /// </summary>
    /// <param name="input">The incoming event.</param>
    /// <param name="context">The execution context.</param>
    /// <returns>Object conveying SQS message item failures.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there is no registered implementation of <see cref="IMessageHandler{TMessage}"/>.</exception>
    /// <seealso href="https://aws.amazon.com/about-aws/whats-new/2021/11/aws-lambda-partial-batch-response-sqs-event-source/"/>
    async Task<SQSBatchResponse> IRequestResponseHandler<SQSEvent, SQSBatchResponse>.HandleAsync(SQSEvent? input, ILambdaContext context)
    {
        return new(await HandleAsync(input, context));
    }

    private async Task<List<BatchItemFailure>> HandleAsync(SQSEvent? input, ILambdaContext context)
    {
        if (input is { Records.Count: > 0 })
        {
            var eventContext = new EventContext(input, _logger, context); // for ISqsEventLogger
            await using var batchScopeServices = await _serviceProvider.CreateBatchScopeAsync(eventContext).ConfigureAwait(false);
            using var scope = _serviceProvider.CreateScope();
            var serializer = scope.ServiceProvider.GetRequiredService<IMessageSerializer>();
            var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();

            var messageList = new List<TMessage>(input.Records.Count);
            foreach (var record in input.Records)
            {
                var sqsMessage = record.Body;
                TMessage? message;

                try
                {
                    message = serializer.Deserialize<TMessage>(sqsMessage);
                }
                catch (Exception exc)
                {
                    _logger.LogError(exc, "One of the messages in the batch ({messageId}) failed to deserialize.", record.MessageId);
                    throw;
                }

                if (message is null)
                {
                    _logger.LogError("One of the messages in the batch ({messageId}) deserialized to null", record.MessageId);
                    throw new InvalidOperationException($"One of the messages in the batch ({record.MessageId}) deserialized to null.");
                }

                messageList.Add(message);
            }

            var messages = new Messages<TMessage>(messageList);
            _logger.LogInformation("Invoking notification handler for manually processed batch of {count} messages", messages.Count);

            var successIndices = (await handler.HandleAsync(messages, context).ConfigureAwait(false)).ToHashSet();
            var batchItemFailures = from index in Enumerable.Range(0, input.Records.Count)
                                    where !successIndices.Contains(index)
                                    let record = input.Records[index]
                                    let messageId = record.MessageId
                                    select new BatchItemFailure() { ItemIdentifier = messageId };

            var result = batchItemFailures.ToList();
            if (result.Count > 0)
            {
                _logger.LogWarning("Reporting {failureCount} failures to AWS", result.Count);
            }

            return result;
        }
        else
        {
            return new();
        }
    }
}