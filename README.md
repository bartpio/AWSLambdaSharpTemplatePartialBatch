# AWSLambdaSharpTemplatePartialBatch

## Overview

This library adds [partial batch support for SQS-triggered Lambdas](https://aws.amazon.com/about-aws/whats-new/2021/11/aws-lambda-partial-batch-response-sqs-event-source/) to Kralizek's excellent [AWSLambdaSharpTemplate](https://github.com/Kralizek/AWSLambdaSharpTemplate).

## SQS Partial Batch Quick Start

 - Call `UsePartialBatchQueueMessageHandler` instead of `UseQueueMessageHandler`
 - Derive your entry point class from `PartialBatchEventFunction` instead of `EventFunction<SQSEvent>`
 - Write your `IMessageHandler<TInput>` (and provide its type to `UsePartialBatchQueueMessageHandler`) as usual
 - Ensure that the SQS event source is configured for partial batch support from the AWS side (the next section describes how this can be done)

`UsePartialBatchQueueMessageHandler` adds partial batch support by registering an `IRequestResponseHandler<SQSEvent, SQSBatchResponse>` (alongside the usual `IEventHandler<SQSEvent>`). Any exceptions thrown by your handler will be caught, logged, and provided to AWS, using a `SQSBatchResponse` (generated on your behalf) that conveys which messages failed to process.

## Handling SQS messages with partial batch responses

The [SQS Event Source](https://docs.aws.amazon.com/lambda/latest/dg/with-sqs.html) can be configured to send batches of more than one message to the Lambda. When a class that derives from `EventFunction<SQSEvent>` is used as the Lambda entry point, any exceptions thrown from the message handler will propagate to the Lambda runtime, causing the entire batch that's being processed by that Lambda invocation to fail. All of the messages in the failed batch will be retried (subject to SQS configuration).

As an alternative to this default behavior, [partial batch support](https://aws.amazon.com/about-aws/whats-new/2021/11/aws-lambda-partial-batch-response-sqs-event-source/) can be enabled by deriving the entry point class from `PartialBatchEventFunction` (or `RequestResponseFunction<SQSEvent, SQSBatchResponse>`) instead, and configuring the SQS Event Source to look in the Lambda response body for batch item failure information. When partial batch support is enabled, exceptions thrown from the handler are caught, and failed messages are reported to Lambda in the response payload. Only failed messages will be retried (subject to SQS configuration).

The Event Source configuration can be done using [CloudFormation](https://docs.aws.amazon.com/AWSCloudFormation/latest/UserGuide/aws-resource-lambda-eventsourcemapping.html#cfn-lambda-eventsourcemapping-functionresponsetypes), or by turning on "Report batch item failures" in the AWS console (trigger configuration), or by some other means such as the AWS CLI.

Configuring the SQS Event Source is critical to partial batch support working properly. If this step is omitted, yet an entry point class deriving from `RequestResponseFunction<SQSEvent, SQSBatchResponse>` is used, exceptions thrown from the message handler will be logged but then ignored (not retried).

## Parallel execution of SQS messages

Parallel processing is supported, and configured the same way as with the baseline `AWSLambdaSharpTemplate` library. Please see [Parallel execution of SQS messages](https://github.com/Kralizek/AWSLambdaSharpTemplate#parallel-execution-of-sqs-messages).

## SQS Partial Batch Event Log

Optionally, implementations of `ISqsEventLogger` can be registered, to inspect the progress of batches and individual messages. An instance of each `ISqsEventLogger` implementation registered as transient will be created for each batch. Raw `SQSEvent` and `SQSEvent.SQSMessage` instances are provided for inspection.

 - `BatchReceivedAsync` is called at the start of a batch.
 - `MessageReceivedAsync` is called for each message in the batch, before processing begins.
 - `PartialBatchItemFailureAsync` is called for each message that experienced a failure. The exception thrown from the message handler is supplied as an argument.
 - `MessageCompletedAsync` is called for each message, after the message is done processing, or the message fails to process succesfully.
 - `BatchCompletedAsync` is called after all messages in a batch have been processed.

`MessageReceivedAsync`, `PartialBatchItemFailureAsync`, and `MessageCompletedAsync` may be called from various threads simultaneously, when parallel execution is enabled.

## Manual Batch Processing

When processing an entire batch at once is desired, use interfaces and extension methods in the `Kralizek.Lambda.PartialBatch.ManualBatch` namespace. Build an implementation of `Kralizek.Lambda.PartialBatch.ManualBatch.IMessageHandler` that receives a list of messages, and returns the indices of succesfully processed messages. Provide its type to the `UsePartialBatchQueueMessageHandler` extension method. The library will return the identifiers of messages *not* succesfully processed to AWS.
