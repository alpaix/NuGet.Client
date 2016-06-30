// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using NuGet.PackageManagement.UI;
using NuGet.PackageManagement.VisualStudio;

namespace NuGetVSExtension
{
    internal class ActionEventSink : IActionEventSink
    {
        // keeps a reference to BuildEvents so that our event handler
        // won't get disconnected because of GC.
        private readonly BuildEvents _buildEvents;
        private readonly SolutionEvents _solutionEvents;
        private readonly ErrorListProvider _errors;

        public ActionEventSink(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            _errors = new ErrorListProvider(serviceProvider);

            var dte = ServiceLocator.GetInstance<DTE>();

            _buildEvents = dte.Events.BuildEvents;
            _buildEvents.OnBuildBegin += (_, __) => { _errors.Tasks.Clear(); };
            _solutionEvents = dte.Events.SolutionEvents;
            _solutionEvents.AfterClosing += () => { _errors.Tasks.Clear(); };
        }

        public void OnActionCompleted()
        {
            if (_errors.Tasks.Count > 0)
            {
                _errors.BringToFront();
                _errors.ForceShowErrors();
            }
        }

        public void OnActionStarted()
        {
            _errors.Tasks.Clear();
        }

        public void OnError(string message)
        {
            AddTask(message, TaskErrorCategory.Error);
        }

        public void OnWarning(string message)
        {
            AddTask(message, TaskErrorCategory.Warning);
        }

        private void AddTask(string message, TaskErrorCategory errorCategory)
        {
            var errorTask = new ErrorTask
            {
                Text = message,
                ErrorCategory = errorCategory,
                Category = TaskCategory.User,
                Priority = TaskPriority.High,
                HierarchyItem = null,
                HelpKeyword = "NU101"
            };

            _errors.Tasks.Add(errorTask);
        }
    }
}