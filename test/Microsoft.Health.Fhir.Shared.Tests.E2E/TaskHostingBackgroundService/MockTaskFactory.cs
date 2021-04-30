﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Threading;
using Microsoft.Health.TaskManagement;

namespace Microsoft.Health.Fhir.Shared.Tests.E2E.TaskHostingBackgroundService
{
    public enum MockTaskType
    {
        Fail = 100,
        Success,
        Loop,
    }

    public class MockTaskFactory : ITaskFactory
    {
        private IContextUpdaterFactory _contextUpdaterFactory;

        public MockTaskFactory(IContextUpdaterFactory contextUpdaterFactory)
        {
            _contextUpdaterFactory = contextUpdaterFactory;
        }

        public ITask Create(TaskInfo taskInfo)
        {
            IContextUpdater contextUpdater = _contextUpdaterFactory.CreateContextUpdater(taskInfo.TaskId, taskInfo.RunId);
            contextUpdater.UpdateContextAsync(taskInfo.RetryCount.ToString(), CancellationToken.None);

            if (taskInfo.TaskTypeId == (short)MockTaskType.Fail)
            {
                return new MockFailueTask
                {
                    RunId = taskInfo.RunId,
                };
            }
            else if (taskInfo.TaskTypeId == (short)MockTaskType.Success)
            {
                return new MockSuccessTask
                {
                    RunId = taskInfo.RunId,
                };
            }
            else
            {
                return new MockLoopTask
                {
                    RunId = taskInfo.RunId,
                };
            }
        }
    }
}
