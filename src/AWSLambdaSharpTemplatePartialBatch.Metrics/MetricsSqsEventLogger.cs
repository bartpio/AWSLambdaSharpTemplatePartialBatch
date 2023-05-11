using System.Collections.Concurrent;
using System.Diagnostics;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.Lambda.Core;
using Kralizek.Lambda.PartialBatch.EventLog;
using Microsoft.Extensions.Logging;

namespace Kralizek.Lambda.PartialBatch.Metrics
{
    /// <summary>
    /// Writes metrics regarding message processing within SQS-triggered partial batch Lambdas.
    /// </summary>
    public class MetricsSqsEventLogger : ISqsEventLogger
    {
        private readonly ILogger _logger;
        private readonly IAmazonCloudWatch _cloud;
        private List<Dimension> _dimensions = null!; // set from BatchReceivedAsync
        private ConcurrentBag<MetricDatum> _data = null!; // set from BatchReceivedAsync
        private Stopwatch[] _stopwatches = null!; // set from BatchReceivedAsync

        /// <summary>
        /// Construct an instance. Generally invoked using DI.
        /// </summary>
        public MetricsSqsEventLogger(ILogger<MetricsSqsEventLogger> logger, IAmazonCloudWatch cloud)
        {
            _logger = logger;
            _cloud = cloud;
        }

        ValueTask ISqsEventLogger.BatchReceivedAsync(EventContext eventContext)
        {
            _dimensions = BuildDimensions(eventContext.LambdaContext);
            _data = new();
            _stopwatches = new Stopwatch[eventContext.Event.Records.Count];

            return ValueTask.CompletedTask;
        }

        private static List<Dimension> BuildDimensions(ILambdaContext lc)
        {
            return new()
            {
                new() { Name = "FunctionName", Value = lc.FunctionName},
                new() { Name = "ExecutedVersion", Value = lc.FunctionVersion},
            };
        }

        ValueTask ISqsEventLogger.MessageReceivedAsync(EventContext eventContext, MessageContext messageContext)
        {
            _data.Add(BuildDatum("PartialBatchItemRx"));
            _stopwatches[messageContext.Index] = Stopwatch.StartNew();
            return ValueTask.CompletedTask;
        }

        public MetricDatum BuildDatum(string name)
        {
            return new() { Dimensions = _dimensions, MetricName = name, StorageResolution = 1, TimestampUtc = DateTime.UtcNow, Unit = StandardUnit.Count, Value = 1 };
        }

        ValueTask ISqsEventLogger.MessageCompletedAsync(EventContext eventContext, MessageContext messageContext)
        {
            var span = _stopwatches[messageContext.Index].Elapsed;
            _data.Add(BuildDatum("PartialBatchItemInvocations"));
            _data.Add(BuildDatum("PartialBatchItemDuration", span));
            return ValueTask.CompletedTask;
        }

        public MetricDatum BuildDatum(string name, TimeSpan span)
        {
            return new() { Dimensions = _dimensions, MetricName = name, StorageResolution = 1, TimestampUtc = DateTime.UtcNow, Unit = StandardUnit.Milliseconds, Value = span.TotalMilliseconds };
        }

        ValueTask ISqsEventLogger.PartialBatchItemFailureAsync(EventContext eventContext, MessageContext messageContext, Exception exc)
        {
            _data.Add(BuildDatum("PartialBatchItemErrors"));
            return ValueTask.CompletedTask;
        }

        async ValueTask ISqsEventLogger.BatchCompletedAsync(EventContext eventContext)
        {
            var req = new PutMetricDataRequest() { Namespace = "AWS/Lambda", MetricData = _data.ToList() };
            var remaining = eventContext.LambdaContext.RemainingTime - TimeSpan.FromSeconds(2);

            try
            {
                using var cts = new CancellationTokenSource(remaining);
                await _cloud.PutMetricDataAsync(req, cts.Token);
            }
            catch (Exception exc)
            {
                _logger.LogWarning(exc, "could not write partial batch metrics");
            }
        }
    }
}
