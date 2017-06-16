// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using NuGet.PackageManagement.VisualStudio;
using NuGet.VisualStudio;

namespace NuGetConsole.Host.PowerShell
{
    internal class AsyncPowerShellHost : PowerShellHost
    {
        public override bool IsAsync => true;
        public override event EventHandler ExecuteEnd;

        public AsyncPowerShellHost(IRestoreEvents restoreEvents, IRunspaceManager runspaceManager)
            : base(restoreEvents, runspaceManager)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        protected override bool ExecuteHost(string fullCommand, string command, params object[] inputs)
        {
            SetPrivateDataOnHost(false);

            try
            {
                var pipeline = Runspace.InvokeAsync(fullCommand, inputs, true, (sender, e) =>
                    {
                        switch (e.PipelineStateInfo.State)
                        {
                            case PipelineState.Completed:
                            case PipelineState.Failed:
                            case PipelineState.Stopped:
                                if (e.PipelineStateInfo.Reason != null)
                                {
                                    ReportError(e.PipelineStateInfo.Reason);
                                }

                                ExecuteEnd.Raise(this, EventArgs.Empty);
                                break;
                        }
                    });

                ExecutingPipeline = pipeline;
                return true;
            }
            catch (RuntimeException e)
            {
                ReportError(e.ErrorRecord);
                ExceptionHelper.WriteErrorToActivityLog(e);
            }
            catch (Exception e)
            {
                ReportError(e);
                ExceptionHelper.WriteErrorToActivityLog(e);
            }

            return false; // Error occurred, command not executing
        }
    }
}
