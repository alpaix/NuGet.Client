// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using Microsoft;
using NuGet.VisualStudio;
using NuGetConsole.Host.PowerShell;

namespace NuGetConsole.Host
{
    [Export(typeof(IHostProvider))]
    [HostName(HostName)]
    [DisplayName("NuGet Provider")]
    internal class PowerShellHostProvider : IHostProvider
    {
        /// <summary>
        /// PowerConsole host name of PowerShell host.
        /// </summary>
        /// <remarks>
        /// </remarks>
        public const string HostName = "NuGetConsole.Host.PowerShell";

        private readonly IRunspaceManager _runspaceManager;
        private readonly IRestoreEvents _restoreEvents;
        
        [ImportingConstructor]
        public PowerShellHostProvider(
            IRunspaceManager runspaceManager,
            IRestoreEvents restoreEvents)
        {
            Assumes.Present(runspaceManager);
            Assumes.Present(restoreEvents);

            _runspaceManager = runspaceManager;
            _restoreEvents = restoreEvents;
        }

        public IHost CreateHost(bool @async)
        {
            var isPowerShell2Installed = RegistryHelper.CheckIfPowerShell2OrAboveInstalled();
            if (isPowerShell2Installed)
            {
                return CreatePowerShellHost(@async);
            }
            return new UnsupportedHost();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IHost CreatePowerShellHost(bool isAsync)
        {
            // backdoor: allow turning off async mode by setting enviroment variable NuGetSyncMode=1
            var syncModeFlag = Environment.GetEnvironmentVariable("NuGetSyncMode", EnvironmentVariableTarget.User);
            if (syncModeFlag == "1")
            {
                isAsync = false;
            }

            IHost host;
            if (isAsync)
            {
                host = new AsyncPowerShellHost(_restoreEvents, _runspaceManager);
            }
            else
            {
                host = new SyncPowerShellHost(_restoreEvents, _runspaceManager);
            }

            return host;
        }
    }
}
