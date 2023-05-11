using Amazon.CloudWatch;
using Kralizek.Lambda.PartialBatch.EventLog;
using Microsoft.Extensions.DependencyInjection;

namespace Kralizek.Lambda.PartialBatch.Metrics
{
    /// <summary>
    /// Extensions for registering metrics collection regarding message processing within SQS-triggered partial batch Lambdas.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add metrics collection regarding message processing within SQS-triggered partial batch Lambdas.
        /// </summary>
        public static IServiceCollection AddPartialBatchMetrics(this IServiceCollection services)
        {
            services.AddAWSService<IAmazonCloudWatch>();
            services.AddTransient<ISqsEventLogger, MetricsSqsEventLogger>();
            return services;
        }
    }
}
