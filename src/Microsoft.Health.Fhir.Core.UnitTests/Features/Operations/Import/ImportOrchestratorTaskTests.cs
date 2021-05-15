﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Health.Core;
using Microsoft.Health.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Context;
using Microsoft.Health.Fhir.Core.Features.Operations;
using Microsoft.Health.Fhir.Core.Features.Operations.Import;
using Microsoft.Health.Fhir.Core.Features.Operations.Import.Models;
using Microsoft.Health.TaskManagement;
using Newtonsoft.Json;
using NSubstitute;
using Xunit;

namespace Microsoft.Health.Fhir.Core.UnitTests.Features.Operations.Import
{
    public class ImportOrchestratorTaskTests
    {
        [Fact]
        public async Task GivenAnOrchestratorTask_WhenProcessingInputFilesMoreThanConcurrentCount_ThenTaskShouldBeCompleted()
        {
            await VerifyCommonOrchestratorTaskAsync(105, 6);
        }

        [Fact]
        public async Task GivenAnOrchestratorTask_WhenProcessingInputFilesEqualsConcurrentCount_ThenTaskShouldBeCompleted()
        {
            await VerifyCommonOrchestratorTaskAsync(105, 105);
        }

        [Fact]
        public async Task GivenAnOrchestratorTask_WhenProcessingInputFilesLessThanConcurrentCount_ThenTaskShouldBeCompleted()
        {
            await VerifyCommonOrchestratorTaskAsync(11, 105);
        }

        [Fact]
        public async Task GivenAnOrchestratorTaskAndWrongEtag_WhenOrchestratorTaskStart_ThenTaskShouldFailedWithDetails()
        {
            IFhirDataBulkImportOperation fhirDataBulkImportOperation = Substitute.For<IFhirDataBulkImportOperation>();
            IContextUpdater contextUpdater = Substitute.For<IContextUpdater>();
            RequestContextAccessor<IFhirRequestContext> contextAccessor = Substitute.For<RequestContextAccessor<IFhirRequestContext>>();
            ILoggerFactory loggerFactory = new NullLoggerFactory();
            IIntegrationDataStoreClient integrationDataStoreClient = Substitute.For<IIntegrationDataStoreClient>();
            ISequenceIdGenerator<long> sequenceIdGenerator = Substitute.For<ISequenceIdGenerator<long>>();
            ImportOrchestratorTaskInputData importOrchestratorTaskInputData = new ImportOrchestratorTaskInputData();
            ImportOrchestratorTaskContext importOrchestratorTaskContext = new ImportOrchestratorTaskContext();
            ITaskManager taskManager = Substitute.For<ITaskManager>();

            importOrchestratorTaskInputData.TaskId = Guid.NewGuid().ToString("N");
            importOrchestratorTaskInputData.TaskCreateTime = Clock.UtcNow;
            importOrchestratorTaskInputData.BaseUri = new Uri("http://dummy");
            var inputs = new List<InputResource>();
            inputs.Add(new InputResource() { Type = "Resource", Url = new Uri("http://dummy"), Etag = "dummy" });
            importOrchestratorTaskInputData.Input = inputs;
            importOrchestratorTaskInputData.InputFormat = "ndjson";
            importOrchestratorTaskInputData.InputSource = new Uri("http://dummy");
            importOrchestratorTaskInputData.MaxConcurrentProcessingTaskCount = 1;
            importOrchestratorTaskInputData.MaxConcurrentRebuildIndexOperationCount = 3;
            importOrchestratorTaskInputData.ProcessingTaskQueueId = "default";
            importOrchestratorTaskInputData.RequestUri = new Uri("http://dummy");

            integrationDataStoreClient.GetPropertiesAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties[IntegrationDataStoreClientConstants.BlobPropertyETag] = "test";
                    properties[IntegrationDataStoreClientConstants.BlobPropertyLength] = 1000L;
                    return properties;
                });

            sequenceIdGenerator.GetCurrentSequenceId().Returns(_ => 0L);

            ImportOrchestratorTask orchestratorTask = new ImportOrchestratorTask(
                importOrchestratorTaskInputData,
                importOrchestratorTaskContext,
                taskManager,
                sequenceIdGenerator,
                contextUpdater,
                contextAccessor,
                fhirDataBulkImportOperation,
                integrationDataStoreClient,
                loggerFactory);

            TaskResultData result = await orchestratorTask.ExecuteAsync();
            ImportTaskErrorResult resultDetails = JsonConvert.DeserializeObject<ImportTaskErrorResult>(result.ResultData);

            Assert.Equal(TaskResult.Fail, result.Result);
            Assert.Equal(HttpStatusCode.BadRequest, resultDetails.HttpStatusCode);
            Assert.NotEmpty(resultDetails.ErrorMessage);
        }

        [Fact]
        public async Task GivenAnOrchestratorTask_WhenIntegrationExceptionThrow_ThenTaskShouldFailedWithDetails()
        {
            IFhirDataBulkImportOperation fhirDataBulkImportOperation = Substitute.For<IFhirDataBulkImportOperation>();
            IContextUpdater contextUpdater = Substitute.For<IContextUpdater>();
            RequestContextAccessor<IFhirRequestContext> contextAccessor = Substitute.For<RequestContextAccessor<IFhirRequestContext>>();
            ILoggerFactory loggerFactory = new NullLoggerFactory();
            IIntegrationDataStoreClient integrationDataStoreClient = Substitute.For<IIntegrationDataStoreClient>();
            ISequenceIdGenerator<long> sequenceIdGenerator = Substitute.For<ISequenceIdGenerator<long>>();
            ImportOrchestratorTaskInputData importOrchestratorTaskInputData = new ImportOrchestratorTaskInputData();
            ImportOrchestratorTaskContext importOrchestratorTaskContext = new ImportOrchestratorTaskContext();
            ITaskManager taskManager = Substitute.For<ITaskManager>();

            importOrchestratorTaskInputData.TaskId = Guid.NewGuid().ToString("N");
            importOrchestratorTaskInputData.TaskCreateTime = Clock.UtcNow;
            importOrchestratorTaskInputData.BaseUri = new Uri("http://dummy");
            var inputs = new List<InputResource>();
            inputs.Add(new InputResource() { Type = "Resource", Url = new Uri("http://dummy"), Etag = "dummy" });
            importOrchestratorTaskInputData.Input = inputs;
            importOrchestratorTaskInputData.InputFormat = "ndjson";
            importOrchestratorTaskInputData.InputSource = new Uri("http://dummy");
            importOrchestratorTaskInputData.MaxConcurrentProcessingTaskCount = 1;
            importOrchestratorTaskInputData.MaxConcurrentRebuildIndexOperationCount = 3;
            importOrchestratorTaskInputData.ProcessingTaskQueueId = "default";
            importOrchestratorTaskInputData.RequestUri = new Uri("http://dummy");

            integrationDataStoreClient.GetPropertiesAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
                .Returns<Task<Dictionary<string, object>>>(_ =>
                {
                    throw new IntegrationDataStoreException("dummy", HttpStatusCode.Unauthorized);
                });

            sequenceIdGenerator.GetCurrentSequenceId().Returns(_ => 0L);

            ImportOrchestratorTask orchestratorTask = new ImportOrchestratorTask(
                importOrchestratorTaskInputData,
                importOrchestratorTaskContext,
                taskManager,
                sequenceIdGenerator,
                contextUpdater,
                contextAccessor,
                fhirDataBulkImportOperation,
                integrationDataStoreClient,
                loggerFactory);

            TaskResultData result = await orchestratorTask.ExecuteAsync();
            ImportTaskErrorResult resultDetails = JsonConvert.DeserializeObject<ImportTaskErrorResult>(result.ResultData);

            Assert.Equal(TaskResult.Fail, result.Result);
            Assert.Equal(HttpStatusCode.Unauthorized, resultDetails.HttpStatusCode);
            Assert.NotEmpty(resultDetails.ErrorMessage);
        }

        [Fact]
        public async Task GivenAnOrchestratorTask_WhenFailedAtPreprocessStep_ThenRetrableExceptionShouldBeThrowAndContextUpdated()
        {
            IFhirDataBulkImportOperation fhirDataBulkImportOperation = Substitute.For<IFhirDataBulkImportOperation>();
            IContextUpdater contextUpdater = Substitute.For<IContextUpdater>();
            RequestContextAccessor<IFhirRequestContext> contextAccessor = Substitute.For<RequestContextAccessor<IFhirRequestContext>>();
            ILoggerFactory loggerFactory = new NullLoggerFactory();
            IIntegrationDataStoreClient integrationDataStoreClient = Substitute.For<IIntegrationDataStoreClient>();
            ISequenceIdGenerator<long> sequenceIdGenerator = Substitute.For<ISequenceIdGenerator<long>>();
            ImportOrchestratorTaskInputData importOrchestratorTaskInputData = new ImportOrchestratorTaskInputData();
            ImportOrchestratorTaskContext importOrchestratorTaskContext = new ImportOrchestratorTaskContext();
            List<(long begin, long end)> surrogatedIdRanges = new List<(long begin, long end)>();
            TestTaskManager taskManager = new TestTaskManager(t =>
            {
                if (t == null)
                {
                    return null;
                }

                t.Status = TaskManagement.TaskStatus.Running;
                return t;
            });

            importOrchestratorTaskInputData.TaskId = Guid.NewGuid().ToString("N");
            importOrchestratorTaskInputData.TaskCreateTime = Clock.UtcNow;
            importOrchestratorTaskInputData.BaseUri = new Uri("http://dummy");
            var inputs = new List<InputResource>();
            inputs.Add(new InputResource() { Type = "Resource", Url = new Uri($"http://dummy") });

            importOrchestratorTaskInputData.Input = inputs;
            importOrchestratorTaskInputData.InputFormat = "ndjson";
            importOrchestratorTaskInputData.InputSource = new Uri("http://dummy");
            importOrchestratorTaskInputData.MaxConcurrentProcessingTaskCount = 1;
            importOrchestratorTaskInputData.MaxConcurrentRebuildIndexOperationCount = 3;
            importOrchestratorTaskInputData.ProcessingTaskQueueId = "default";
            importOrchestratorTaskInputData.RequestUri = new Uri("http://dummy");

            integrationDataStoreClient.GetPropertiesAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties[IntegrationDataStoreClientConstants.BlobPropertyETag] = "test";
                    properties[IntegrationDataStoreClientConstants.BlobPropertyLength] = 1000L;
                    return properties;
                });

            fhirDataBulkImportOperation.DisableIndexesAsync(Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    throw new InvalidCastException();
                });

            string latestContext = null;
            contextUpdater.UpdateContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    latestContext = (string)callInfo[0];
                    return Task.CompletedTask;
                });

            sequenceIdGenerator.GetCurrentSequenceId().Returns(_ => 0L);

            ImportOrchestratorTask orchestratorTask = new ImportOrchestratorTask(
                importOrchestratorTaskInputData,
                importOrchestratorTaskContext,
                taskManager,
                sequenceIdGenerator,
                contextUpdater,
                contextAccessor,
                fhirDataBulkImportOperation,
                integrationDataStoreClient,
                loggerFactory);
            orchestratorTask.PollingFrequencyInSeconds = 0;

            await Assert.ThrowsAnyAsync<RetriableTaskException>(() => orchestratorTask.ExecuteAsync());
            ImportOrchestratorTaskContext context = JsonConvert.DeserializeObject<ImportOrchestratorTaskContext>(latestContext);
            Assert.Equal(ImportOrchestratorTaskProgress.InputResourcesValidated, context.Progress);
        }

        [Fact]
        public async Task GivenAnOrchestratorTask_WhenFailedAtGenerateSubTasksStep_ThenRetrableExceptionShouldBeThrowAndContextUpdated()
        {
            IFhirDataBulkImportOperation fhirDataBulkImportOperation = Substitute.For<IFhirDataBulkImportOperation>();
            IContextUpdater contextUpdater = Substitute.For<IContextUpdater>();
            RequestContextAccessor<IFhirRequestContext> contextAccessor = Substitute.For<RequestContextAccessor<IFhirRequestContext>>();
            ILoggerFactory loggerFactory = new NullLoggerFactory();
            IIntegrationDataStoreClient integrationDataStoreClient = Substitute.For<IIntegrationDataStoreClient>();
            ISequenceIdGenerator<long> sequenceIdGenerator = Substitute.For<ISequenceIdGenerator<long>>();
            ImportOrchestratorTaskInputData importOrchestratorTaskInputData = new ImportOrchestratorTaskInputData();
            ImportOrchestratorTaskContext importOrchestratorTaskContext = new ImportOrchestratorTaskContext();
            List<(long begin, long end)> surrogatedIdRanges = new List<(long begin, long end)>();
            TestTaskManager taskManager = new TestTaskManager(t =>
            {
                if (t == null)
                {
                    return null;
                }

                t.Status = TaskManagement.TaskStatus.Running;
                return t;
            });

            importOrchestratorTaskInputData.TaskId = Guid.NewGuid().ToString("N");
            importOrchestratorTaskInputData.TaskCreateTime = Clock.UtcNow;
            importOrchestratorTaskInputData.BaseUri = new Uri("http://dummy");
            var inputs = new List<InputResource>();
            inputs.Add(new InputResource() { Type = "Resource", Url = new Uri($"http://dummy") });

            importOrchestratorTaskInputData.Input = inputs;
            importOrchestratorTaskInputData.InputFormat = "ndjson";
            importOrchestratorTaskInputData.InputSource = new Uri("http://dummy");
            importOrchestratorTaskInputData.MaxConcurrentProcessingTaskCount = 1;
            importOrchestratorTaskInputData.MaxConcurrentRebuildIndexOperationCount = 3;
            importOrchestratorTaskInputData.ProcessingTaskQueueId = "default";
            importOrchestratorTaskInputData.RequestUri = new Uri("http://dummy");

            integrationDataStoreClient.GetPropertiesAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties[IntegrationDataStoreClientConstants.BlobPropertyETag] = "test";
                    properties[IntegrationDataStoreClientConstants.BlobPropertyLength] = 1000L;
                    return properties;
                });

            string latestContext = null;
            contextUpdater.UpdateContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    latestContext = (string)callInfo[0];
                    return Task.CompletedTask;
                });

            sequenceIdGenerator.GetCurrentSequenceId().Returns<long>(_ => throw new InvalidOperationException());

            ImportOrchestratorTask orchestratorTask = new ImportOrchestratorTask(
                importOrchestratorTaskInputData,
                importOrchestratorTaskContext,
                taskManager,
                sequenceIdGenerator,
                contextUpdater,
                contextAccessor,
                fhirDataBulkImportOperation,
                integrationDataStoreClient,
                loggerFactory);
            orchestratorTask.PollingFrequencyInSeconds = 0;

            await Assert.ThrowsAnyAsync<RetriableTaskException>(() => orchestratorTask.ExecuteAsync());
            ImportOrchestratorTaskContext context = JsonConvert.DeserializeObject<ImportOrchestratorTaskContext>(latestContext);
            Assert.Equal(ImportOrchestratorTaskProgress.PreprocessCompleted, context.Progress);
        }

        [Fact]
        public async Task GivenAnOrchestratorTask_WhenFailedAtMonitorSubTasksStep_ThenRetrableExceptionShouldBeThrowAndContextUpdated()
        {
            IFhirDataBulkImportOperation fhirDataBulkImportOperation = Substitute.For<IFhirDataBulkImportOperation>();
            IContextUpdater contextUpdater = Substitute.For<IContextUpdater>();
            RequestContextAccessor<IFhirRequestContext> contextAccessor = Substitute.For<RequestContextAccessor<IFhirRequestContext>>();
            ILoggerFactory loggerFactory = new NullLoggerFactory();
            IIntegrationDataStoreClient integrationDataStoreClient = Substitute.For<IIntegrationDataStoreClient>();
            ISequenceIdGenerator<long> sequenceIdGenerator = Substitute.For<ISequenceIdGenerator<long>>();
            ImportOrchestratorTaskInputData importOrchestratorTaskInputData = new ImportOrchestratorTaskInputData();
            ImportOrchestratorTaskContext importOrchestratorTaskContext = new ImportOrchestratorTaskContext();
            List<(long begin, long end)> surrogatedIdRanges = new List<(long begin, long end)>();
            TestTaskManager taskManager = new TestTaskManager(t =>
            {
                throw new InvalidOperationException();
            });

            importOrchestratorTaskInputData.TaskId = Guid.NewGuid().ToString("N");
            importOrchestratorTaskInputData.TaskCreateTime = Clock.UtcNow;
            importOrchestratorTaskInputData.BaseUri = new Uri("http://dummy");
            var inputs = new List<InputResource>();
            inputs.Add(new InputResource() { Type = "Resource", Url = new Uri($"http://dummy") });

            importOrchestratorTaskInputData.Input = inputs;
            importOrchestratorTaskInputData.InputFormat = "ndjson";
            importOrchestratorTaskInputData.InputSource = new Uri("http://dummy");
            importOrchestratorTaskInputData.MaxConcurrentProcessingTaskCount = 1;
            importOrchestratorTaskInputData.MaxConcurrentRebuildIndexOperationCount = 3;
            importOrchestratorTaskInputData.ProcessingTaskQueueId = "default";
            importOrchestratorTaskInputData.RequestUri = new Uri("http://dummy");

            integrationDataStoreClient.GetPropertiesAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties[IntegrationDataStoreClientConstants.BlobPropertyETag] = "test";
                    properties[IntegrationDataStoreClientConstants.BlobPropertyLength] = 1000L;
                    return properties;
                });

            string latestContext = null;
            contextUpdater.UpdateContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    latestContext = (string)callInfo[0];
                    return Task.CompletedTask;
                });

            sequenceIdGenerator.GetCurrentSequenceId().Returns<long>(_ => 0L);

            ImportOrchestratorTask orchestratorTask = new ImportOrchestratorTask(
                importOrchestratorTaskInputData,
                importOrchestratorTaskContext,
                taskManager,
                sequenceIdGenerator,
                contextUpdater,
                contextAccessor,
                fhirDataBulkImportOperation,
                integrationDataStoreClient,
                loggerFactory);
            orchestratorTask.PollingFrequencyInSeconds = 0;

            await Assert.ThrowsAnyAsync<RetriableTaskException>(() => orchestratorTask.ExecuteAsync());
            ImportOrchestratorTaskContext context = JsonConvert.DeserializeObject<ImportOrchestratorTaskContext>(latestContext);
            Assert.Equal(ImportOrchestratorTaskProgress.SubTaskRecordsGenerated, context.Progress);
        }

        [Fact]
        public async Task GivenAnOrchestratorTask_WhenFailedAtPostProcessStep_ThenRetrableExceptionShouldBeThrowAndContextUpdated()
        {
            IFhirDataBulkImportOperation fhirDataBulkImportOperation = Substitute.For<IFhirDataBulkImportOperation>();
            IContextUpdater contextUpdater = Substitute.For<IContextUpdater>();
            RequestContextAccessor<IFhirRequestContext> contextAccessor = Substitute.For<RequestContextAccessor<IFhirRequestContext>>();
            ILoggerFactory loggerFactory = new NullLoggerFactory();
            IIntegrationDataStoreClient integrationDataStoreClient = Substitute.For<IIntegrationDataStoreClient>();
            ISequenceIdGenerator<long> sequenceIdGenerator = Substitute.For<ISequenceIdGenerator<long>>();
            ImportOrchestratorTaskInputData importOrchestratorTaskInputData = new ImportOrchestratorTaskInputData();
            ImportOrchestratorTaskContext importOrchestratorTaskContext = new ImportOrchestratorTaskContext();
            List<(long begin, long end)> surrogatedIdRanges = new List<(long begin, long end)>();
            TestTaskManager taskManager = new TestTaskManager(t =>
            {
                if (t == null)
                {
                    return null;
                }

                ImportProcessingTaskInputData processingInput = JsonConvert.DeserializeObject<ImportProcessingTaskInputData>(t.InputData);
                ImportProcessingTaskResult processingResult = new ImportProcessingTaskResult();
                processingResult.ResourceType = processingInput.ResourceType;
                processingResult.SucceedCount = 1;
                processingResult.FailedCount = 1;
                processingResult.ErrorLogLocation = "http://dummy/error";
                surrogatedIdRanges.Add((processingInput.BeginSequenceId, processingInput.EndSequenceId));

                t.Result = JsonConvert.SerializeObject(new TaskResultData(TaskResult.Success, JsonConvert.SerializeObject(processingResult)));
                t.Status = TaskManagement.TaskStatus.Completed;
                return t;
            });

            importOrchestratorTaskInputData.TaskId = Guid.NewGuid().ToString("N");
            importOrchestratorTaskInputData.TaskCreateTime = Clock.UtcNow;
            importOrchestratorTaskInputData.BaseUri = new Uri("http://dummy");
            var inputs = new List<InputResource>();
            inputs.Add(new InputResource() { Type = "Resource", Url = new Uri($"http://dummy") });

            importOrchestratorTaskInputData.Input = inputs;
            importOrchestratorTaskInputData.InputFormat = "ndjson";
            importOrchestratorTaskInputData.InputSource = new Uri("http://dummy");
            importOrchestratorTaskInputData.MaxConcurrentProcessingTaskCount = 1;
            importOrchestratorTaskInputData.MaxConcurrentRebuildIndexOperationCount = 3;
            importOrchestratorTaskInputData.ProcessingTaskQueueId = "default";
            importOrchestratorTaskInputData.RequestUri = new Uri("http://dummy");

            integrationDataStoreClient.GetPropertiesAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties[IntegrationDataStoreClientConstants.BlobPropertyETag] = "test";
                    properties[IntegrationDataStoreClientConstants.BlobPropertyLength] = 1000L;
                    return properties;
                });

            string latestContext = null;
            contextUpdater.UpdateContextAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    latestContext = (string)callInfo[0];
                    return Task.CompletedTask;
                });

            fhirDataBulkImportOperation.RebuildIndexesAsync(Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(_ =>
                {
                    throw new InvalidCastException();
                });

            sequenceIdGenerator.GetCurrentSequenceId().Returns<long>(_ => 0L);

            ImportOrchestratorTask orchestratorTask = new ImportOrchestratorTask(
                importOrchestratorTaskInputData,
                importOrchestratorTaskContext,
                taskManager,
                sequenceIdGenerator,
                contextUpdater,
                contextAccessor,
                fhirDataBulkImportOperation,
                integrationDataStoreClient,
                loggerFactory);
            orchestratorTask.PollingFrequencyInSeconds = 0;

            await Assert.ThrowsAnyAsync<RetriableTaskException>(() => orchestratorTask.ExecuteAsync());
            ImportOrchestratorTaskContext context = JsonConvert.DeserializeObject<ImportOrchestratorTaskContext>(latestContext);
            Assert.Equal(ImportOrchestratorTaskProgress.SubTasksCompleted, context.Progress);
            Assert.Equal(1, context.DataProcessingTasks.Count);
        }

        private static async Task VerifyCommonOrchestratorTaskAsync(int inputFileCount, int concurrentCount)
        {
            IFhirDataBulkImportOperation fhirDataBulkImportOperation = Substitute.For<IFhirDataBulkImportOperation>();
            IContextUpdater contextUpdater = Substitute.For<IContextUpdater>();
            RequestContextAccessor<IFhirRequestContext> contextAccessor = Substitute.For<RequestContextAccessor<IFhirRequestContext>>();
            ILoggerFactory loggerFactory = new NullLoggerFactory();
            IIntegrationDataStoreClient integrationDataStoreClient = Substitute.For<IIntegrationDataStoreClient>();
            ISequenceIdGenerator<long> sequenceIdGenerator = Substitute.For<ISequenceIdGenerator<long>>();
            ImportOrchestratorTaskInputData importOrchestratorTaskInputData = new ImportOrchestratorTaskInputData();
            ImportOrchestratorTaskContext importOrchestratorTaskContext = new ImportOrchestratorTaskContext();
            List<(long begin, long end)> surrogatedIdRanges = new List<(long begin, long end)>();
            TestTaskManager taskManager = new TestTaskManager(t =>
            {
                if (t == null)
                {
                    return null;
                }

                ImportProcessingTaskInputData processingInput = JsonConvert.DeserializeObject<ImportProcessingTaskInputData>(t.InputData);
                ImportProcessingTaskResult processingResult = new ImportProcessingTaskResult();
                processingResult.ResourceType = processingInput.ResourceType;
                processingResult.SucceedCount = 1;
                processingResult.FailedCount = 1;
                processingResult.ErrorLogLocation = "http://dummy/error";
                surrogatedIdRanges.Add((processingInput.BeginSequenceId, processingInput.EndSequenceId));

                t.Result = JsonConvert.SerializeObject(new TaskResultData(TaskResult.Success, JsonConvert.SerializeObject(processingResult)));
                t.Status = TaskManagement.TaskStatus.Completed;
                return t;
            });

            importOrchestratorTaskInputData.TaskId = Guid.NewGuid().ToString("N");
            importOrchestratorTaskInputData.TaskCreateTime = Clock.UtcNow;
            importOrchestratorTaskInputData.BaseUri = new Uri("http://dummy");
            var inputs = new List<InputResource>();
            for (int i = 0; i < inputFileCount; ++i)
            {
                inputs.Add(new InputResource() { Type = "Resource", Url = new Uri($"http://dummy/{i}") });
            }

            importOrchestratorTaskInputData.Input = inputs;
            importOrchestratorTaskInputData.InputFormat = "ndjson";
            importOrchestratorTaskInputData.InputSource = new Uri("http://dummy");
            importOrchestratorTaskInputData.MaxConcurrentProcessingTaskCount = concurrentCount;
            importOrchestratorTaskInputData.MaxConcurrentRebuildIndexOperationCount = 3;
            importOrchestratorTaskInputData.ProcessingTaskQueueId = "default";
            importOrchestratorTaskInputData.RequestUri = new Uri("http://dummy");

            integrationDataStoreClient.GetPropertiesAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
                .Returns(callInfo =>
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    properties[IntegrationDataStoreClientConstants.BlobPropertyETag] = "test";
                    properties[IntegrationDataStoreClientConstants.BlobPropertyLength] = 1000L;
                    return properties;
                });

            sequenceIdGenerator.GetCurrentSequenceId().Returns(_ => 0L);

            ImportOrchestratorTask orchestratorTask = new ImportOrchestratorTask(
                importOrchestratorTaskInputData,
                importOrchestratorTaskContext,
                taskManager,
                sequenceIdGenerator,
                contextUpdater,
                contextAccessor,
                fhirDataBulkImportOperation,
                integrationDataStoreClient,
                loggerFactory);
            orchestratorTask.PollingFrequencyInSeconds = 0;

            TaskResultData result = await orchestratorTask.ExecuteAsync();
            ImportTaskResult resultDetails = JsonConvert.DeserializeObject<ImportTaskResult>(result.ResultData);
            Assert.Equal(TaskResult.Success, result.Result);
            Assert.Equal(inputFileCount, resultDetails.Output.Count);
            foreach (ImportOperationOutcome outcome in resultDetails.Output)
            {
                Assert.Equal(1, outcome.Count);
                Assert.NotNull(outcome.InputUrl);
                Assert.NotEmpty(outcome.Type);
            }

            Assert.Equal(inputFileCount, resultDetails.Error.Count);
            foreach (ImportOperationOutcome outcome in resultDetails.Error)
            {
                Assert.Equal(1, outcome.Count);
                Assert.NotNull(outcome.InputUrl);
                Assert.NotEmpty(outcome.Type);
                Assert.NotEmpty(outcome.Url);
            }

            Assert.NotEmpty(resultDetails.Request);
            Assert.Equal(importOrchestratorTaskInputData.TaskCreateTime, resultDetails.TransactionTime);

            var orderedSurrogatedIdRanges = surrogatedIdRanges.OrderBy(r => r.begin).ToArray();
            Assert.Equal(inputFileCount, orderedSurrogatedIdRanges.Length);
            for (int i = 0; i < orderedSurrogatedIdRanges.Length - 1; ++i)
            {
                Assert.True(orderedSurrogatedIdRanges[i].end > orderedSurrogatedIdRanges[i].begin);
                Assert.True(orderedSurrogatedIdRanges[i].end <= orderedSurrogatedIdRanges[i + 1].begin);
            }
        }
    }
}