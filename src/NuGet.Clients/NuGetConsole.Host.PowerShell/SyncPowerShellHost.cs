// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using NuGet.PackageManagement.VisualStudio;
using NuGet.VisualStudio;

namespace NuGetConsole.Host.PowerShell
{
    internal class SyncPowerShellHost : PowerShellHost
    {
        public override bool IsAsync => false;
        public override event EventHandler ExecuteEnd;

        public SyncPowerShellHost(IRestoreEvents restoreEvents, IRunspaceManager runspaceManager)
            : base(restoreEvents, runspaceManager)
        {
        }

        protected override bool ExecuteHost(string fullCommand, string command, params object[] inputs)
        {
            SetPrivateDataOnHost(true);

            try
            {
                Runspace.Invoke(fullCommand, inputs, true);
                ExecuteEnd.Raise(this, EventArgs.Empty);
            }
            catch (Exception e)
            {
                ExceptionHelper.WriteErrorToActivityLog(e);
                throw;
            }

            return true;
        }
    }
}
